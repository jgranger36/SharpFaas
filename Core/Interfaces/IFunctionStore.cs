namespace Core.Interfaces;

public interface IFunctionStore
{
    string LocalStorePath { get; }
    string RemoteStorePath { get; }

    Task<Function> PushFunctionAsync(Function function, bool newVersion, Stream zipArchive);
    Task<string> GetFunctionAsync(Function function);
}