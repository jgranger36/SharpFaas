namespace Core.Helpers;

public static class VersionHelpers
{
    public static Version ToVersion(this string versionString)
    {
        var version = new Version(0,0,0,0);

        if (!string.IsNullOrWhiteSpace(versionString))
        {
            var versionArray = versionString.Split('.');
            if (versionArray.Length > 0 && int.TryParse(versionArray[0], out int major))
            {
                version = new Version(major, 0);

                if (versionArray.Length > 1 && int.TryParse(versionArray[1], out int minor))
                {
                    version = new Version(major, minor);
                
                    if (versionArray.Length > 2 && int.TryParse(versionArray[2], out int build))
                    {
                        version = new Version(major, minor,build);
                    
                        if (versionArray.Length > 3 && int.TryParse(versionArray[3], out int revision))
                        {
                            version = new Version(major, minor,build,revision);
                        }
                    }
                }
               
            } 
        }

        return version;
    }

    public static Version BumpMajorVersion(this Version version)
    {
        version = version.NullSafeVersion();

        version = new Version(version.Major + 1, version.Minor, version.Build, version.Revision);

        return version;
    }
    
    public static Version BumpMinorVersion(this Version version)
    {
        version = version.NullSafeVersion();

        version = new Version(version.Major, version.Minor+ 1, version.Build, version.Revision);

        return version;
    }
    
    public static Version BumpBuildVersion(this Version version)
    {
        version = version.NullSafeVersion();

        version = new Version(version.Major, version.Minor, version.Build+ 1, version.Revision);

        return version;
    }
    
    public static Version BumpRevisionVersion(this Version version)
    {
        version = version.NullSafeVersion();

        version = new Version(version.Major, version.Minor, version.Build, version.Revision+ 1);

        return version;
    }

    public static Version NullSafeVersion(this Version version)
    {
        if (version is null)
            return new Version(0, 0, 0, 0);

        return version;
    }

    public static Version DefaultVersion => new Version(0, 0, 0, 0);
}