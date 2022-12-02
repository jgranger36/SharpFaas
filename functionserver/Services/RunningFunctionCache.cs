using Core;

namespace functionserver.Services;

public class RunningFunctionCache
{
    public Dictionary<Guid, (Task task, Function function,DateTime startTime,CancellationTokenSource cancellationTokenSource)> _cache =
        new Dictionary<Guid, (Task task, Function function,DateTime startTime,CancellationTokenSource cancellationTokenSource)>();
}