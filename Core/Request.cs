namespace Core;

public class Request
{
    public string FunctionName { get; set; }
    public string CallerId { get; set; }
    public string InstanceId { get; set; }
    public string Lane { get; set; }
    public string Version { get; set; }
    public FunctionFile.Extension Type { get; set; }
    public string EntryFileName { get; set; }
    public string Payload { get; set; }

}