namespace NetClaw.Docker.Extensions;

public sealed class SandboxFileService(SandboxPathResolver pathResolver)
{
    private readonly string _sharedDir = pathResolver.GetSharedDir();

    public string SharedDir => _sharedDir;

    public string GetDownloadsDir()
    {
        var path = Path.Combine(_sharedDir, "downloads");
        Directory.CreateDirectory(path);
        return path;
    }

    public string BuildSafeDownloadPath(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "download.xlsx";
        }

        return Path.Combine(GetDownloadsDir(), safeName);
    }
}
