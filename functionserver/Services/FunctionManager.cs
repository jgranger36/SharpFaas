namespace functionserver.Services;

public class FunctionManager
{
    private RunningFunctionCache _runningFunctionCache;
    
    public FunctionManager(RunningFunctionCache runningFunctionCache)
    {
        _runningFunctionCache = runningFunctionCache;

        ExecuteAsync();
    }
    
    protected async Task ExecuteAsync()
    {
        while (true)
        {
            _runningFunctionCache.Clean();
            
            Task.Delay(15 * 60000).Wait();
        }
    }
}