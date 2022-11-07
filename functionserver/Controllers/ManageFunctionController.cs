using System.Runtime.CompilerServices;
using Core;
using Core.Helpers;
using Core.Interfaces;
using functionserver.Services;
using Microsoft.AspNetCore.Mvc;

namespace functionserver.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class FunctionController : ControllerBase
{
    private readonly ILogger<FunctionController> _logger;
    private IFunctionStore _functionStore;
    private FunctionExecutor _functionExecutor;
    private Encryption _encryption;

    public FunctionController(ILogger<FunctionController> logger,IFunctionStore functionStore,Encryption encryption,FunctionExecutor functionExecutor)
    {
        _logger = logger;
        _functionStore = functionStore;
        _encryption = encryption;
        _functionExecutor = functionExecutor;
    }

    [HttpPost(Name = "pushfunction")]
    public async Task<IActionResult> PushFunction(string functionName,string type, string lane,bool newVersion,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        Function.Extension.TryParse(type, out Function.Extension extension);
        
        var function = new Function(functionName,extension ,lane, String.Empty);
        
        if (file.Length <= 0)
            return BadRequest("Empty file");

        if (file.FileName.EndsWith("zip"))
        {
            var response =
                await _functionStore.PushFunctionAsync(function, newVersion, file.OpenReadStream());
            
            return Ok($"Saved function {functionName} to function store in lane: {lane} as version {function.Version.ToString()}");
        }
        else
        {
            return BadRequest(new {message = "Invalid file extension. This endpoint only supports .zip files."});
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    [HttpPost(Name = "runfunction")]
    public async Task<IActionResult> RunFunction(
        string callerId,
        string functionName,
        string lane,
        string version,
        string payload,
        bool payLoadEncrypted,
        string type,
        string entryFileName = null)
    {
        try
        {
            Function.Extension.TryParse(type, out Function.Extension extension);

            if (payLoadEncrypted)
                payload = _encryption.DecryptString(payload);

            if (extension is Function.Extension.dll)
                await _functionExecutor.FunctionHandler(functionName, lane, version, payload);

            else if (extension is Function.Extension.exe)
                await _functionExecutor.ProcessHandler(functionName, lane, version,
                    payload,entryFileName);
            else
            {
                _logger.LogError($"Passed in FuntionType is not a valid FunctionType: CallerID:{callerId} FunctionName:{functionName} - Lane:{lane} - Version:{version} - Type:{type}");
                return BadRequest($"Passed in FuntionType is not a valid FunctionType: {type}");
            }
              

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,$"Error in running function: CallerID:{callerId} FunctionName:{functionName} - Lane:{lane} - Version:{version} - Type:{type}");
            return BadRequest(ex.Message);
        }
    }
}