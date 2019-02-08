using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using TorrentCreatorUploaderLogic;

namespace TorrentCreatorUploaderService
{
    public partial class TorrentCreatorUploaderService : ServiceBase
    {
        private readonly List<FSystemWatcher> _fsWatchers = new List<FSystemWatcher>();

        public TorrentCreatorUploaderService()
        {
            var paths = ConfigurationManager.AppSettings["DownloadFolder"]
                .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var path in paths)
            {
                var fsWatcher = new FSystemWatcher(path);
                _fsWatchers.Add(fsWatcher);
            }

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            foreach (var fsWatcher in _fsWatchers) fsWatcher.StartFsWatcherService();
        }

        protected override void OnStop()
        {
            foreach (var fsWatcher in _fsWatchers) fsWatcher.StopFsWatcherService();
        }
    }
}