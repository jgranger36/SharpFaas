using System.Text.Json;
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
        FunctionFile.Extension.TryParse(type, out FunctionFile.Extension extension);

        var function = new FunctionFile(functionName, extension, lane, String.Empty, null);

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

        string functionName,
        [FromBody] JsonElement payload,
        string? callerId,
        Guid? instanceId,
        string? lane = "latest",
        string? version = "0",
        bool? payLoadEncrypted = false,
        string? type = "dll")
    {
        instanceId = instanceId.HasValue ? instanceId.Value : Guid.NewGuid();
        var headers = HttpContext.Request.Headers;
        var parameters = HttpContext.Request.Query;
        var cancellationTokenSource = new CancellationTokenSource();
        var startTime = DateTime.UtcNow;
        var function = new FunctionFile(functionName, FunctionFile.Extension.dll, lane, version, new Payload
        {
            Url = HttpContext.Request.QueryString.ToString(),
            Headers = headers.ToDictionary(h => h.Key.ToString(),h => h.Value.ToString()),
            Parameters = parameters.ToDictionary(q => q.Key.ToString(), q => q.Value.ToString()),
            Body = payload.GetRawText()
        });
        
        var response = new Response()
        {
            CallerId = callerId, FunctionName = functionName, Lane = lane, Version = version, Type = type,
            InstanceId = instanceId.Value
        };
        
        try
        {
            if (FunctionFile.Extension.TryParse(type, out FunctionFile.Extension extension))
            {
                function.FileExtension = extension;

                var pathToAssembly = await _functionStore.GetFunctionAsync(function);

                if (string.IsNullOrWhiteSpace(pathToAssembly))
                    throw new Exception("Could not find assembly.");

                var token = cancellationTokenSource.Token;

                var runningFunction = new RunningFunctionCache.RunningFunction
                {
                    RunningFunctionId = instanceId.Value,
                    CallerId = callerId,
                    Function = function,
                    StartTime = startTime,
                    PathToAssembly = pathToAssembly,
                    Action = () => _functionExecutor.ProcessHandler(function, pathToAssembly,token),
                    TokenSource = cancellationTokenSource
                };

                if (extension is FunctionFile.Extension.dll)
                    runningFunction.Action = () => _functionExecutor.FunctionHandler(function, pathToAssembly,token);

                else if (extension is FunctionFile.Extension.exe)
                    runningFunction.Action = () => _functionExecutor.ProcessHandler(function, pathToAssembly,token);
                
                _runningFunctionCache.Add(runningFunction);

                response.ElapsedTimeInSeconds = Convert.ToInt32((DateTime.UtcNow - startTime).TotalSeconds);
                response.Version = function.Version.ToString();
                response.TaskStatus = TaskStatus.WaitingToRun.ToString();

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

        RunningFunctionCache.RunningFunction? runningFunction = _runningFunctionCache.Get(instanceId);
        
        if (runningFunction != null)
        {
            return Ok(JsonConvert.SerializeObject(new
            {
                InstanceId = instanceId,
                CallerId = runningFunction.CallerId,
                FunctionName = runningFunction.Function.Name,
                Lane = runningFunction.Function.Lane,
                Version = runningFunction.Function.Version,
                Type = runningFunction.Function.FileExtension.ToString(),
                TaskStatus = runningFunction.Task.Status.ToString(),
                Exceptions = runningFunction.Task.Exception != null
                    ? runningFunction.Task.Exception.InnerExceptions.Select(e => e.Message).ToList()
                        .Concat(new List<string>() {runningFunction.Task.Exception.Message})
                    : null,
                ElapsedTimeInSeconds = Convert.ToInt32((DateTime.UtcNow - runningFunction.StartTime).TotalSeconds)
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
        var runningFunctions = _runningFunctionCache.GetAll();
        return Ok(JsonConvert.SerializeObject(runningFunctions.Select(f => new
        {
            InstanceId = f.RunningFunctionId,
            CallerId = f.CallerId,
            FunctionName = f.Function.Name,
            Lane = f.Function.Lane,
            Version = f.Function.Version,
            Type = f.Function.FileExtension.ToString(),
            TaskStatus = f.Task.Status.ToString(),
            Exceptions = f.Task.Exception != null
                ? f.Task.Exception.InnerExceptions.Select(e => e.Message).ToList()
                    .Concat(new List<string>() {f.Task.Exception.Message})
                : null,
            ElapsedTimeInSeconds = Convert.ToInt32((DateTime.UtcNow - f.StartTime).TotalSeconds)
        })));
    }

    [HttpPut(Name = "cancelfunction")]
    public async Task<IActionResult> CancelFunction(Guid instanceId)
    {
        _runningFunctionCache.Remove(instanceId);
        
        return Ok(JsonConvert.SerializeObject(new
        {
            InstanceId = instanceId
        }));
    }

    [HttpGet(Name = "getserverstatus")]
    public async Task<IActionResult> GetServerStatus()
    {
        return Ok(new {Status = "Active"});
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