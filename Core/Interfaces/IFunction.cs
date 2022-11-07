namespace Core.Interfaces;

public interface IFunction 
{
    Guid FunctionId { get; }
    Guid InstanceId { get; }
    string Label { get; }
    string Description { get; }
    DateTime StartTime { get; }
    StringWriter FunctionConsole { get; set; }


    Task ExecuteAsync(string jsonPayload,StringWriter writer = null);
}