using System.Diagnostics;
using System.Runtime.CompilerServices;
using Core.Interfaces;
using Core;
using McMaster.NETCore.Plugins;

namespace functionserver.Services;

public class FunctionExecutor
{
    private ILogger _logger;
    private Configuration _config;
    private IFunctionStore _functionStore;

    public FunctionExecutor(Configuration config, IFunctionStore functionStore, ILogger logger)
    {
        _logger = logger;
        _config = config;
        _functionStore = functionStore;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async IAsyncEnumerable<string> FunctionHandlerAsync(
        string functionName,
        string lane,
        string version,
        string payload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        PluginLoader loader = null;
        Function function = new Function(functionName, Function.Extension.dll, lane, version);
        try
        {
            using (var console = new FunctionConsole())
            {
                var pathToAssembly = await _functionStore.GetFunctionAsync(function);

                loader = PluginLoader.CreateFromAssemblyFile(
                    assemblyFile: Path.GetFullPath(pathToAssembly),
                    sharedTypes: new[] {typeof(IFunction)},
                    isUnloadable: true);

                var functionType = loader
                    .LoadDefaultAssembly()
                    .GetTypes()
                    .FirstOrDefault(t => typeof(IFunction).IsAssignableFrom(t));


                IFunction iFunction = (IFunction) Activator.CreateInstance(functionType);

                var task = iFunction.ExecuteAsync(payload, console);

                if (task != null)
                {
                    while (!task.IsCompleted || console.HasText())
                    {
                        yield return console.ReadAll();

                        await Task.Delay(1000);
                    }
                }
            }
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task FunctionHandler(
        string functionName,
        string lane,
        string version,
        string payload)
    {
        PluginLoader loader = null;
        Function function = new Function(functionName, Function.Extension.dll, lane, version);
        try
        {
            var pathToAssembly = await _functionStore.GetFunctionAsync(function);

            loader = PluginLoader.CreateFromAssemblyFile(
                assemblyFile: Path.GetFullPath(pathToAssembly),
                sharedTypes: new[] {typeof(IFunction)},
                isUnloadable: true);

            var functionType = loader
                .LoadDefaultAssembly()
                .GetTypes()
                .FirstOrDefault(t => typeof(IFunction).IsAssignableFrom(t));


            IFunction iFunction = (IFunction) Activator.CreateInstance(functionType);

            await iFunction.ExecuteAsync(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in running function: {functionName}");
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


    [MethodImpl(MethodImplOptions.NoInlining)]
    public async IAsyncEnumerable<string> ProcessHandlerAsync(
        string functionName,
        string lane,
        string version,
        string payload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Function function = new Function(functionName, Function.Extension.exe, lane, version);
        try
        {
            var pathToAssembly = await _functionStore.GetFunctionAsync(function);

            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var fullTempPath = Directory.CreateDirectory(Path.Combine(tempPath, function.LocalDirectory));

            File.Copy(pathToAssembly, Path.Combine(tempPath, function.File));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pathToAssembly,
                    Arguments = payload,
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();

            yield return "Completed";
        }
        finally
        {
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task ProcessHandler(
        string functionName,
        string lane,
        string version,
        string payload,
        string entryFileName  = null)
    {
        Function function = new Function(functionName, Function.Extension.exe, lane, version,entryFileName);
        try
        {
            var pathToAssembly = await _functionStore.GetFunctionAsync(function);

            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var fullTempPath = Directory.CreateDirectory(Path.Combine(tempPath, function.LocalDirectory));

            File.Copy(pathToAssembly, Path.Combine(tempPath, function.File));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pathToAssembly,
                    Arguments = payload,
                    UseShellExecute = true
                }
            };

            process.Start();
            process.WaitForExit();
        }
        finally
        {
        }
    }
}