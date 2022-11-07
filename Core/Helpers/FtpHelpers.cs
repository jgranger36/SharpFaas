namespace Core.Helpers;

public static class FtpHelpers
{
    public static string ToFtpPath(this string path)
    {
        return path.Replace("\\", "/");
    }
}