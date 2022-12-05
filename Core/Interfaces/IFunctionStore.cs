namespace Core.Interfaces;

public interface IFunctionStore
{
    string LocalStorePath { get; }
    string RemoteStorePath { get; }

    Task<FunctionFile> PushFunctionAsync(FunctionFile function, bool newVersion, Stream zipArchive);
    Task<string> GetFunctionAsync(FunctionFile function);
}