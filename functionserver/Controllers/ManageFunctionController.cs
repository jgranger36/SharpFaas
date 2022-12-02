using Core;
using Core.Helpers;
using Core.Interfaces;
using functionserver.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace functionserver.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class FunctionController : ControllerBase
{
    private readonly ILogger<FunctionController> _logger;
    private IFunctionStore _functionStore;
    private FunctionExecutor _functionExecutor;
    private Encryption _encryption;
    private RunningFunctionCache _runningFunctionCache;

    public FunctionController(ILogger<FunctionController> logger, IFunctionStore functionStore, Encryption encryption,
        FunctionExecutor functionExecutor, RunningFunctionCache runningFunctionCache)
    {
        _logger = logger;
        _functionStore = functionStore;
        _encryption = encryption;
        _functionExecutor = functionExecutor;
        _runningFunctionCache = runningFunctionCache;
    }

    [HttpPost(Name = "pushfunction")]
    public async Task<IActionResult> PushFunction(string functionName, string type, string lane, bool newVersion,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        Function.Extension.TryParse(type, out Function.Extension extension);

        var function = new Function(functionName, extension, lane, String.Empty, null);

        if (file.Length <= 0)
            return BadRequest("Empty file");

        if (file.FileName.EndsWith("zip"))
        {
            var response =
                await _functionStore.PushFunctionAsync(function, newVersion, file.OpenReadStream());

            return Ok(
                $"Saved function {functionName} to function store in lane: {lane} as version {function.Version.ToString()}");
        }
        else
        {
            return BadRequest(new {message = "Invalid file extension. This endpoint only supports .zip files."});
        }
    }

    [HttpPost(Name = "runfunction")]
    public async Task<IActionResult> RunFunction(
        string callerId,
        Guid? instanceId,
        string functionName,
        string payload,
        string? lane = "latest",
        string? version = "0",
        bool? payLoadEncrypted = false,
        string? type = "dll",
        string? entryFileName = null)
    {
        var file = "";
        Task task = null;
        var cancellationTokenSource = new CancellationTokenSource();
        var startTime = DateTime.UtcNow;
        var function = new Function(functionName, Function.Extension.dll, lane, version, payload, entryFileName);
        var response = new Response()
        {
            CallerId = callerId, FunctionName = functionName, Lane = lane, Version = version, Type = type,
            InstanceId = instanceId.HasValue ? instanceId.Value : Guid.NewGuid()
        };
        try
        {
            if (Function.Extension.TryParse(type, out Function.Extension extension))
            {
                function.FileExtension = extension;

                if (payLoadEncrypted.Value)
                    function.Payload = _encryption.DecryptString(payload);

                file = function.File;

                var pathToAssembly = await _functionStore.GetFunctionAsync(function);

                if (string.IsNullOrWhiteSpace(pathToAssembly))
                    throw new Exception("Could not find assembly.");

                var token = cancellationTokenSource.Token;
                
                if (extension is Function.Extension.dll)
                    task = Task.Run(() => _functionExecutor.FunctionHandler(function, pathToAssembly,token),
                        cancellationTokenSource.Token);

                else if (extension is Function.Extension.exe)
                    task = Task.Run(() => _functionExecutor.ProcessHandler(function, pathToAssembly,token),
                        cancellationTokenSource.Token);

                _runningFunctionCache._cache[response.InstanceId] =
                    (task, function, startTime, cancellationTokenSource);

                response.ElapsedTimeInSeconds = Convert.ToInt32((DateTime.UtcNow - startTime).TotalSeconds);
                response.Version = function.Version.ToString();
                response.TaskStatus = task.Status.ToString();

                return Ok(response.ToString());
            }
            else
            {
                response.Exceptions.Add($"Passed in FuntionType is not a valid FunctionType.");
                response.TaskStatus = TaskStatus.Faulted.ToString();
                return BadRequest(response.ToString());
            }
        }
        catch (Exception ex)
        {
            if (_runningFunctionCache._cache.ContainsKey(instanceId.Value))
            {
                if (!task.IsCompleted)
                    cancellationTokenSource.Cancel();
                _runningFunctionCache._cache.Remove(instanceId.Value);
            }

            response.ElapsedTimeInSeconds = Convert.ToInt32((DateTime.UtcNow - startTime).TotalSeconds);
            response.Version = function.Version.ToString();
            response.TaskStatus = TaskStatus.Faulted.ToString();

            response.Exceptions.Add($"Error in processing function. {ex.Message}");
            _logger.LogError(ex, "Error in processing function");
            return BadRequest(response.ToString());
        }
    }

    [HttpGet(Name = "checkfunction")]
    public async Task<IActionResult> CheckFunction(Guid instanceId)
    {
        if (_runningFunctionCache._cache.TryGetValue(instanceId,
                out (Task Task, Function function, DateTime startTime, CancellationTokenSource cancellationTokenSource)
                value))
        {
            if (value.Task.IsCompleted)
                _runningFunctionCache._cache.Remove(instanceId);

            return Ok(JsonConvert.SerializeObject(new
            {
                InstanceId = instanceId,
                FunctionName = value.function.Name,
                Lane = value.function.Lane,
                Version = value.function.Version,
                Type = value.function.FileExtension.ToString(),
                TaskStatus = value.Task.Status.ToString(),
                Exceptions = value.Task.Exception != null
                    ? value.Task.Exception.InnerExceptions.Select(e => e.Message).ToList()
                        .Concat(new List<string>() {value.Task.Exception.Message})
                    : null,
                ElapsedTimeInSeconds = Convert.ToInt32((DateTime.UtcNow - value.startTime).TotalSeconds)
            }));
        }

        return NotFound(JsonConvert.SerializeObject(new
        {
            InstanceId = instanceId,
            Exceptions = new List<string>()
            {
                "No InstanceId was found matching passed in value"
            }
        }));
    }

    [HttpGet(Name = "getactivefunctions")]
    public async Task<IActionResult> GetActiveFunctions()
    {
        return Ok(JsonConvert.SerializeObject(_runningFunctionCache._cache.Select(f => new
        {
            InstanceId = f.Key,
            FunctionName = f.Value.function.Name,
            Lane = f.Value.function.Lane,
            Version = f.Value.function.Version,
            Type = f.Value.function.FileExtension.ToString(),
            TaskStatus = f.Value.task.Status.ToString(),
            Exceptions = f.Value.task.Exception != null
                ? f.Value.task.Exception.InnerExceptions.Select(e => e.Message).ToList()
                    .Concat(new List<string>() {f.Value.task.Exception.Message})
                : null,
            ElapsedTimeInSeconds = Convert.ToInt32((DateTime.UtcNow - f.Value.startTime).TotalSeconds)
        })));
    }

    [HttpPut(Name = "cancelfunction")]
    public async Task<IActionResult> CancelFunction(Guid instanceId)
    {
        if (_runningFunctionCache._cache.TryGetValue(instanceId,
                out (Task Task, Function function, DateTime startTime, CancellationTokenSource cancellationTokenSource)
                value))
        {
            if (!value.Task.IsCompleted)
            {
                value.cancellationTokenSource.Cancel();

                while (!value.Task.IsCanceled)
                {
                    await Task.Delay(100);
                }
            }

            _runningFunctionCache._cache.Remove(instanceId);

            return Ok(JsonConvert.SerializeObject(new
            {
                InstanceId = instanceId,
                FunctionName = value.function.Name,
                Lane = value.function.Lane,
                Version = value.function.Version,
                Type = value.function.FileExtension.ToString(),
                TaskStatus = value.Task.Status.ToString(),
                Exceptions = value.Task.Exception != null
                    ? value.Task.Exception.InnerExceptions.Select(e => e.Message).ToList()
                        .Concat(new List<string>() {value.Task.Exception.Message})
                    : null,
                ElapsedTimeInSeconds = Convert.ToInt32((DateTime.UtcNow - value.startTime).TotalSeconds)
            }));
        }
        else
        {
            return Ok(JsonConvert.SerializeObject(new
            {
                InstanceId = instanceId,
                FunctionName = "",
                Lane = "",
                Version = "",
                Type = "",
                TaskStatus = "NoTaskFound",
                Exceptions = new List<string>(0),
                ElapsedTimeInSeconds = 0
            }));
        }
    }

    public class Response
    {
        public string CallerId { get; set; }
        public Guid InstanceId { get; set; }
        public string FunctionName { get; set; }
        public string Lane { get; set; }
        public string Version { get; set; }
        public string TaskStatus { get; set; }
        public string Type { get; set; }
        public List<string> Exceptions { get; set; } = new List<string>();
        public int ElapsedTimeInSeconds { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}