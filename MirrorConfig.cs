using System;
using System.IO;
using System.Text.Json;

public class MirrorConfig
{
    public string SourceFolder { get; set; } = "";
    public string TargetFolder { get; set; } = "";
    public int PollIntervalSeconds { get; set; } = 5;
    public bool DeleteExtraFiles { get; set; } = true;
    public string LogPath { get; set; } = "C:\\Tools\\FolderMirror\\foldermirror.log";

    public static MirrorConfig Load(string configPath)
    {
        string fullPath = Path.GetFullPath(configPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Config file not found: {fullPath}");
        }

        string json = File.ReadAllText(fullPath);
        var config = JsonSerializer.Deserialize<MirrorConfig>(json);

        if (config == null)
        {
            throw new InvalidOperationException("Failed to parse appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(config.SourceFolder))
        {
            throw new InvalidOperationException("SourceFolder is empty.");
        }

        if (string.IsNullOrWhiteSpace(config.TargetFolder))
        {
            throw new InvalidOperationException("TargetFolder is empty.");
        }

        if (config.PollIntervalSeconds <= 0)
        {
            config.PollIntervalSeconds = 5;
        }

        return config;
    }
}
