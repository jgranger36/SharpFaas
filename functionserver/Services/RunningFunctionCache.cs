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
            
            _cache[runningFunction.InstanceId] = runningFunction;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public void Remove(Guid instanceId)
    {
        _semaphore.Wait();
        
        try
        {
            if (_cache.TryGetValue(instanceId, out RunningFunction runningFunction) && runningFunction.Task.IsCompleted )
                _cache.Remove(instanceId);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public RunningFunction? Get(Guid instanceId)
    {
        RunningFunction? runningFunction = null;
        _semaphore.Wait();
        
        try
        {
            if (_cache.TryGetValue(instanceId, out runningFunction))
            {

                if (runningFunction.Task.IsCompleted)
                    _cache.Remove(instanceId);
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
                .Where(f => f.Value.Task.IsCompleted && f.Value.StartTime < DateTime.UtcNow.AddMinutes(-15))
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
        public Guid InstanceId { get; set; }
        public string CallerId { get; set; }
        public Task Task { get; set; }
        public FunctionFile Function { get; set; }
        public DateTime StartTime { get; set; }
        public string PathToAssembly { get; set; }
        public Action Action { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }
}