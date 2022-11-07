using System.Runtime.CompilerServices;
using Core;
using Core.Helpers;
using functionserver.Services;
using Microsoft.AspNetCore.SignalR;

namespace functionserver.Hubs
{
    public class FunctionsHub : Hub
    {
        private ILogger _logger;
        private FunctionExecutor _functionExecutor;
        private Encryption _encryption;

        public FunctionsHub(Configuration config,FunctionExecutor functionExecutor, ILogger logger,Encryption encryption)
        {
            _logger = logger;
            _functionExecutor = functionExecutor;
            _encryption = encryption;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async IAsyncEnumerable<string> FunctionHandler(
            string functionName,
            string lane,
            string version,
            string payload,
            bool payLoadEncrypted,
            string type,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            
                Function.Extension.TryParse(type, out Function.Extension extension);
                
            if (payLoadEncrypted)
                payload = _encryption.DecryptString(payload);

            if(extension is Function.Extension.dll)
                await foreach(var response in _functionExecutor.FunctionHandlerAsync(functionName,lane,version,payload,cancellationToken))
                {
                    yield return response;
                }
            else if (extension is Function.Extension.exe)
                await foreach (var response in _functionExecutor.ProcessHandlerAsync(functionName, lane, version,
                                   payload, cancellationToken))
                {
                    yield return response;
                }
            else
                yield return "Incorrect function type passed in.";
        }

    }
}