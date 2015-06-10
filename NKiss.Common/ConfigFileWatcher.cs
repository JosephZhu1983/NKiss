
using System;
using System.IO;
using System.Threading;

namespace NKiss.Common
{
    public delegate void ConfigFileChangedEventHandler(object configFile);
    public sealed class ConfigFileWatcher : IDisposable
    {
        private readonly Timer _timer;

        private readonly FileSystemWatcher _fsw = new FileSystemWatcher();

        private const int TimeoutMilliseconds = 1000;

        
        public ConfigFileWatcher(string configFile, ConfigFileChangedEventHandler configFileChangedEventHandler)
        {
            AttachWatcher(new FileInfo(configFile));
            _timer = new Timer(new TimerCallback(configFileChangedEventHandler), configFile, Timeout.Infinite, Timeout.Infinite);
        }

        private void AttachWatcher(FileInfo configFile)
        {
            _fsw.Path = configFile.DirectoryName;
            _fsw.Filter = configFile.Name;


            _fsw.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName;


            _fsw.Changed += new FileSystemEventHandler(ConfigWatcherHandler_OnChanged);
            _fsw.Created += new FileSystemEventHandler(ConfigWatcherHandler_OnChanged);
            _fsw.Deleted += new FileSystemEventHandler(ConfigWatcherHandler_OnChanged);
            _fsw.Renamed += new RenamedEventHandler(ConfigWatcherHandler_OnRenamed);

            _fsw.EnableRaisingEvents = true;
        }

        private void ConfigWatcherHandler_OnChanged(object source, FileSystemEventArgs e)
        {
            _timer.Change(TimeoutMilliseconds, Timeout.Infinite);
        }

        private void ConfigWatcherHandler_OnRenamed(object source, RenamedEventArgs e)
        {
            _timer.Change(TimeoutMilliseconds, Timeout.Infinite);
        }

        public void Dispose()
        {
            _timer.Dispose();
            _fsw.EnableRaisingEvents = false;
            _fsw.Dispose();
        }
    }
}