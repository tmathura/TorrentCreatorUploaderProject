using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace TorrentCreatorUploaderLogic
{
    public class FSystemWatcher
    {
        private readonly string _downloadFolder;
        private readonly FileSystemWatcher _fsWatcherService = new FileSystemWatcher();
        private Timer _timer;

        public FSystemWatcher(string downloadFolder)
        {
            _downloadFolder = downloadFolder;
            InitializeFsWatcherService();
        }

        private void InitializeFsWatcherService()
        {
            try
            {
                // 
                // FSWatcherService
                // 
                _fsWatcherService.BeginInit();
                _fsWatcherService.IncludeSubdirectories =
                    Convert.ToBoolean(ConfigurationManager.AppSettings["WatchSubDirectories"]);
                _fsWatcherService.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                                                                        | NotifyFilters.Attributes
                                                                        | NotifyFilters.Size
                                                                        | NotifyFilters.LastWrite
                                                                        | NotifyFilters.LastAccess
                                                                        | NotifyFilters.CreationTime
                                                                        | NotifyFilters.Security;

                var fileSystemWatcherServiceOnChanged =
                    Convert.ToBoolean(ConfigurationManager.AppSettings["FileSystemWatcherServiceOnChanged"]);
                if (fileSystemWatcherServiceOnChanged) _fsWatcherService.Changed += FSWatcherService_Changed;

                var fileSystemWatcherServiceOnCreated =
                    Convert.ToBoolean(ConfigurationManager.AppSettings["FileSystemWatcherServiceOnCreated"]);
                if (fileSystemWatcherServiceOnCreated) _fsWatcherService.Created += FSWatcherService_Created;

                var fileSystemWatcherServiceOnDeleted =
                    Convert.ToBoolean(ConfigurationManager.AppSettings["FileSystemWatcherServiceOnDeleted"]);
                if (fileSystemWatcherServiceOnDeleted) _fsWatcherService.Deleted += FSWatcherService_Deleted;

                var fileSystemWatcherServiceOnRenamed =
                    Convert.ToBoolean(ConfigurationManager.AppSettings["FileSystemWatcherServiceOnRenamed"]);
                if (fileSystemWatcherServiceOnRenamed) _fsWatcherService.Renamed += FSWatcherService_Renamed;

                if (!Directory.Exists(_downloadFolder)) Directory.CreateDirectory(_downloadFolder);
                _fsWatcherService.Path = _downloadFolder;

                var fileSystemWatchAndProcessValidFileTimer =
                    Convert.ToInt32(ConfigurationManager.AppSettings["FileSystemWatchAndProcessValidFileTimer"]);

                _timer = new Timer
                {
                    Interval = fileSystemWatchAndProcessValidFileTimer,
                    AutoReset = true
                };
                _timer.Elapsed += timer_Elapsed;
                _timer.Start();

                var useTelegram = Convert.ToBoolean(ConfigurationManager.AppSettings["UseTelegram"]);
                if (useTelegram) new TelegramUtil().Instantiate();

                _fsWatcherService.EndInit();
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log("Error: " + ex.Message + ".", EventLogEntryType.Error);
            }
        }

        public void StartFsWatcherService()
        {
            try
            {
                _fsWatcherService.EnableRaisingEvents = true;
                new TorrentUpload().Log("Torrent creating service started.", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log("Error creating torrent: " + ex.Message + ".", EventLogEntryType.Error);
            }
        }

        public void StopFsWatcherService()
        {
            try
            {
                _fsWatcherService.EnableRaisingEvents = false;
                new TorrentUpload().Log("Torrent creating service stopped.", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log("Error creating torrent: " + ex.Message + ".", EventLogEntryType.Error);
            }
        }

        /* DEFINE WATCHER EVENTS... */

        /// <summary>
        ///     Event occurs when the contents of a File or Directory is changed
        /// </summary>
        private void FSWatcherService_Changed(object sender, FileSystemEventArgs e)
        {
            //code here for newly changed file or directory
            new TorrentUpload().Log(
                $"FSWatcherService_Changed started for newly changed file or directory. Folder: [{_downloadFolder}]",
                EventLogEntryType.Information);
            try
            {
                new TorrentCreate().StartTorrentCreateProcess(e);
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log("Error: " + ex.Message + ".", EventLogEntryType.Error);
            }
        }

        /// <summary>
        ///     Event occurs when the a File or Directory is created
        /// </summary>
        private void FSWatcherService_Created(object sender, FileSystemEventArgs e)
        {
            //code here for newly created file or directory    
            new TorrentUpload().Log(
                $"FSWatcherService_Created started for newly created file or directory. Folder: [{_downloadFolder}]",
                EventLogEntryType.Information);
            try
            {
                new TorrentCreate().StartTorrentCreateProcess(e);
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log("Error: " + ex.Message + ".", EventLogEntryType.Error);
            }
        }

        /// <summary>
        ///     Event occurs when the a File or Directory is deleted
        /// </summary>
        private void FSWatcherService_Deleted(object sender, FileSystemEventArgs e)
        {
            //code here for newly deleted file or directory    
            new TorrentUpload().Log(
                $"FSWatcherService_Deleted started for newly deleted file or directory. Folder: [{_downloadFolder}]",
                EventLogEntryType.Information);
            try
            {
                new TorrentCreate().StartTorrentCreateProcess(e);
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log("Error: " + ex.Message + ".", EventLogEntryType.Error);
            }
        }

        /// <summary>
        ///     Event occurs when the a File or Directory is renamed
        /// </summary>
        private void FSWatcherService_Renamed(object sender, RenamedEventArgs e)
        {
            //code here for newly renamed file or directory 
            new TorrentUpload().Log(
                $"FSWatcherService_Renamed started for newly renamed file or directory. Folder: [{_downloadFolder}]",
                EventLogEntryType.Information);
            try
            {
                new TorrentCreate().StartTorrentCreateProcess(e);
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log("Error: " + ex.Message + ".", EventLogEntryType.Error);
            }
        }

        private static void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                new TorrentUpload().Log("Starting to process queued files.", EventLogEntryType.Information);
                var fileSystemWatchAndProcessValidFileProcessAttempts = Convert.ToInt32(
                    ConfigurationManager.AppSettings["FileSystemWatchAndProcessValidFileProcessAttempts"]);

                var fileList = new TorrentUpload().GetDb(
                    $"SELECT * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND Attempts < {fileSystemWatchAndProcessValidFileProcessAttempts}");

                foreach (var file in fileList)
                    try
                    {
                        var sourceFile = file.FilePath;
                        var fileName = Path.GetFileName(sourceFile);
                        new TorrentUpload().Log($"Processing queued file. Filename: [{fileName}]",
                            EventLogEntryType.Information);
                        new TorrentUpload().InsertOrUpdateDb(
                            $"UPDATE tTorrentCreatorUploaderFiles SET Attempts = {file.Attempts + 1} WHERE ID = {file.Id}");

                        new TorrentCreate().CreateTorrentUpload(sourceFile, fileName, file);

                        new TorrentUpload().InsertOrUpdateDb(
                            $"UPDATE tTorrentCreatorUploaderFiles SET Processed = True, ProcessedDate = NOW() WHERE ID = {file.Id}");
                        new TorrentUpload().Log(
                            $"Queued file processed successfully. Filename: [{fileName}]",
                            EventLogEntryType.Information);
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = ex.Message;
                        new TorrentUpload().Log(
                            $"Queued file not processed successfully, check accdb for error message. Filename: [{file.FilePath}]",
                            EventLogEntryType.Information);
                        new TorrentUpload().InsertOrUpdateDb(
                            $"UPDATE tTorrentCreatorUploaderFiles SET ErrorMessage = '{errorMessage.Replace("'", "''")}' WHERE ID = {file.Id}");
                        throw;
                    }

                new TorrentUpload().Log("Completed processing queued files.", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log("Error: " + ex.Message + ".", EventLogEntryType.Error);
            }
        }
    }
}