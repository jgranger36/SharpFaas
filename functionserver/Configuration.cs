namespace functionserver;

public class Configuration
{
    public string FunctionDirectory { get; set; }
    public FunctionFtp Ftp { get; set; }
}

public class FunctionFtp
{
    public string FtpServer { get; set; }
    public string FtpPort { get; set; }
    public string FtpUser { get; set; }
    public string FtpPassword { get; set; }
    public string FtpFolder { get; set; }
}