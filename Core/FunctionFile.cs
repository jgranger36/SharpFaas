

using Core.Helpers;
using Core.Interfaces;

namespace Core;

public class FunctionFile
{
    public string Name { get; set; }
    public string EntryFileName { get; set; }
    public string Lane { get; set; }
    public Version Version { get; set; }
    public Extension FileExtension { get; set; }
    public IPayload Payload { get; set; }


    public FunctionFile(string name,Extension fileExtension,string lane, string version,IPayload payload)
    {
        Name = name;
        FileExtension = fileExtension;
        Lane = lane;
        EntryFileName = name;
        Version = version.ToVersion();
        Payload = payload;
    }

    public string RemoteDirectory => Path.Combine(Name, Lane);
    public string LocalDirectory => Path.Combine(Name, Lane, Version.ToString());
    public string Zip => Path.Combine(RemoteDirectory, $"{Version.ToString()}.zip");
    public string File => Path.Combine(LocalDirectory,$"{EntryFileName ?? Name }.{FileExtension.ToString()}");

    public enum Extension
    {
        exe,
        dll
    }
}