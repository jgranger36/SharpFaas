namespace Core.Interfaces;

public interface IFunction 
{
    Guid FunctionId { get; }
    string Label { get; }
    string Description { get; }

    Task ExecuteAsync(IPayload payload);
}