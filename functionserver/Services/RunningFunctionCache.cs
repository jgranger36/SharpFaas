using Core;

namespace functionserver.Services;

public class RunningFunctionCache
{
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private Dictionary<Guid, RunningFunction> _cache =
        new Dictionary<Guid, RunningFunction>();

    public void Add(RunningFunction runningFunction)
    {
        _semaphore.Wait();
        try
        {
            runningFunction.Task = Task.Run(runningFunction.Action);
            
            _cache[runningFunction.RunningFunctionId] = runningFunction;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public void Remove(Guid RunningFunctionId)
    {
        _semaphore.Wait();
        
        try
        {
            if (_cache.TryGetValue(RunningFunctionId, out RunningFunction runningFunction) && runningFunction.Task.IsCompleted )
                _cache.Remove(RunningFunctionId);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public RunningFunction? Get(Guid runningFunctionId)
    {
        RunningFunction? runningFunction = null;
        _semaphore.Wait();
        
        try
        {
            if (_cache.TryGetValue(runningFunctionId, out runningFunction))
            {

                if (runningFunction.Task.IsCompleted)
                    _cache.Remove(runningFunctionId);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return runningFunction;
    }
    public List<RunningFunction> GetAll()
    {
        List<RunningFunction> runningFunctions = new List<RunningFunction>();
        
        _semaphore.Wait();
        
        try
        {
            runningFunctions = _cache.Select(f => f.Value).ToList();
        }
        finally
        {
            _semaphore.Release();
        }

        return runningFunctions;
    }
    
    
    
    public void Clean()
    {
        _semaphore.Wait();
        try
        {
            var functionsToRemove = _cache
                .Where(f => f.Value.Task.IsCompleted && f.Value.StartTime < DateTime.UtcNow.AddMinutes(-60))
                .Select(f => f.Key);
            
            if(functionsToRemove != null)
                foreach (var id in functionsToRemove)
                {
                    _cache.Remove(id);
                }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public class RunningFunction
    {
        public Guid RunningFunctionId { get; set; }
        public string CallerId { get; set; }
        public Task Task { get; set; }
        public FunctionFile Function { get; set; }
        public DateTime StartTime { get; set; }
        public string PathToAssembly { get; set; }
        public Action Action { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }
}