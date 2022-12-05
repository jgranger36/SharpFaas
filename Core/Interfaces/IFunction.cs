namespace Core.Interfaces;

public interface IFunction 
{
    Guid FunctionId { get; }
    Guid InstanceId { get; }
    string Label { get; }
    string Description { get; }
    DateTime StartTime { get; }
    
    Task ExecuteAsync(IPayload payload);
}