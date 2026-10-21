using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class MirrorEngine
{
    private readonly MirrorConfig _config;
    private readonly object _lock = new object();
    private DateTime _lastTriggered = DateTime.MinValue;

    public MirrorEngine(MirrorConfig config)
    {
        _config = config;
    }

    public async Task RunAsync()
    {
        if (!Directory.Exists(_config.SourceFolder))
        {
            Directory.CreateDirectory(_config.SourceFolder);
        }

        using var watcher = new FileSystemWatcher(_config.SourceFolder)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.LastWrite
                        | NotifyFilters.Size
                        | NotifyFilters.CreationTime
        };

        watcher.Changed += (s, e) => TriggerSync();
        watcher.Created += (s, e) => TriggerSync();
        watcher.Deleted += (s, e) => TriggerSync();
        watcher.Renamed += (s, e) => TriggerSync();

        watcher.EnableRaisingEvents = true;

        while (true)
        {
            try
            {
                if (CanAccessTarget(_config.TargetFolder))
                {
                    SyncMirror();
                }
                else
                {
                    Log("Target folder is not accessible, skip this round.");
                }
            }
            catch (Exception ex)
            {
                Log("Sync loop error: " + ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(_config.PollIntervalSeconds));
        }
    }

    private void TriggerSync()
    {
        lock (_lock)
        {
            var now = DateTime.Now;
            if ((now - _lastTriggered).TotalSeconds < 2)
            {
                return;
            }

            _lastTriggered = now;

            Task.Run(() =>
            {
                try
                {
                    if (CanAccessTarget(_config.TargetFolder))
                    {
                        SyncMirror();
                    }
                    else
                    {
                        Log("Target folder is not accessible; skip triggered sync.");
                    }
                }
                catch (Exception ex)
                {
                    Log("Triggered sync failed: " + ex.Message);
                }
            });
        }
    }

    private bool CanAccessTarget(string targetFolder)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                return false;
            }

            if (!Directory.Exists(targetFolder))
            {
                return false;
            }

            string probeFile = Path.Combine(targetFolder, ".foldermirror_probe.tmp");
            File.WriteAllText(probeFile, "ok");
            File.Delete(probeFile);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SyncMirror()
    {
        if (!Directory.Exists(_config.SourceFolder))
        {
            Directory.CreateDirectory(_config.SourceFolder);
        }

        if (!Directory.Exists(_config.TargetFolder))
        {
            if (!CanAccessTarget(_config.TargetFolder))
            {
                Log("Target directory does not exist or is not accessible, skip sync.");
                return;
            }

            Directory.CreateDirectory(_config.TargetFolder);
        }

        foreach (string file in Directory.GetFiles(_config.SourceFolder, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(_config.SourceFolder, file);
            string targetFile = Path.Combine(_config.TargetFolder, relativePath);

            string? targetDir = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            if (!File.Exists(targetFile))
            {
                File.Copy(file, targetFile, true);
                File.SetLastWriteTimeUtc(targetFile, File.GetLastWriteTimeUtc(file));
                Log($"Copied new file: {relativePath}");
                continue;
            }

            FileInfo srcInfo = new FileInfo(file);
            FileInfo dstInfo = new FileInfo(targetFile);

            if (srcInfo.LastWriteTimeUtc > dstInfo.LastWriteTimeUtc || srcInfo.Length != dstInfo.Length)
            {
                File.Copy(file, targetFile, true);
                File.SetLastWriteTimeUtc(targetFile, srcInfo.LastWriteTimeUtc);
                Log($"Updated file: {relativePath}");
            }
        }

        if (_config.DeleteExtraFiles)
        {
            DeleteMissingFiles(_config.SourceFolder, _config.TargetFolder);
        }
    }

    private void DeleteMissingFiles(string sourceRoot, string targetRoot)
    {
        foreach (string targetFile in Directory.GetFiles(targetRoot, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(targetRoot, targetFile);
            string sourceFile = Path.Combine(sourceRoot, relativePath);

            if (!File.Exists(sourceFile))
            {
                try
                {
                    File.Delete(targetFile);
                    Log($"Deleted stale file: {relativePath}");
                }
                catch
                {
                    // ignore
                }
            }
        }

        string[] dirs = Directory.GetDirectories(targetRoot, "*", SearchOption.AllDirectories);
        foreach (string dir in dirs.OrderByDescending(d => d.Length))
        {
            string relativePath = Path.GetRelativePath(targetRoot, dir);
            string sourceDir = Path.Combine(sourceRoot, relativePath);

            if (!Directory.Exists(sourceDir))
            {
                try
                {
                    Directory.Delete(dir, true);
                    Log($"Deleted stale directory: {relativePath}");
                }
                catch
                {
                    // ignore
                }
            }
        }
    }

    private void Log(string message)
    {
        try
        {
            string? dir = Path.GetDirectoryName(_config.LogPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.AppendAllText(_config.LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore
        }
    }
}
