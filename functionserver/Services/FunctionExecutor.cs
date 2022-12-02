using System.Diagnostics;
using Core.Interfaces;
using Core;
using McMaster.NETCore.Plugins;

namespace functionserver.Services;

public class FunctionExecutor
{
    private ILogger _logger;
    private IFunctionStore _functionStore;

    public FunctionExecutor(IFunctionStore functionStore, ILogger logger)
    {
        _logger = logger;
        _functionStore = functionStore;
    }
    
    public void FunctionHandler(
        Function function,string pathToAssembly, CancellationToken token)
    {
        PluginLoader loader = null;
        try
        {
            loader = PluginLoader.CreateFromAssemblyFile(
                assemblyFile: Path.GetFullPath(pathToAssembly),
                sharedTypes: new[] {typeof(IFunction)},
                isUnloadable: true);

            var functionType = loader
                .LoadDefaultAssembly()
                .GetTypes()
                .FirstOrDefault(t => typeof(IFunction).IsAssignableFrom(t));


            IFunction iFunction = (IFunction) Activator.CreateInstance(functionType);

            iFunction.ExecuteAsync(function.Payload).Wait(token);
        }
        finally
        {
            if (loader != null)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                loader.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
    
    public void ProcessHandler(
        Function function,string pathToAssembly,CancellationToken token)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var fullTempPath = Directory.CreateDirectory(Path.Combine(tempPath, function.LocalDirectory));

            File.Copy(pathToAssembly, Path.Combine(tempPath, function.File));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pathToAssembly,
                    Arguments = function.Payload,
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExitAsync().Wait(token);
        }
        finally
        {
        }
    }
}