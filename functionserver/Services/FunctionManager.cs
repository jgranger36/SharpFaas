namespace functionserver.Services;

public class FunctionManager : BackgroundService
{
    private RunningFunctionCache _runningFunctionCache;
    
    public FunctionManager(RunningFunctionCache runningFunctionCache)
    {
        _runningFunctionCache = runningFunctionCache;
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Task.Delay(15 * 60000).Wait();
            
            _runningFunctionCache.Clean();
        }

        return Task.CompletedTask;
    }
}