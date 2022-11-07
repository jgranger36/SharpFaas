

using Core.Helpers;

namespace Core;

public class Function
{
    public string Name { get; set; }
    public string EntryFileName { get; set; }
    public string Lane { get; set; }
    public Version Version { get; set; }
    public Extension FileExtension { get; set; }
    

    public Function(string name,Extension fileExtension,string lane, string version,string entryFileName = null)
    {
        Name = name;
        FileExtension = fileExtension;
        Lane = lane;
        EntryFileName = entryFileName;
        Version = version.ToVersion();
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