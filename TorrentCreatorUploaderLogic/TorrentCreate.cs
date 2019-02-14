using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using MonoTorrent.Common;

namespace TorrentCreatorUploaderLogic
{
    public class TorrentCreate
    {
        public void StartTorrentCreateProcess(FileSystemEventArgs e)
        {
            var sourceFile = e.FullPath;
            var fileName = Path.GetFileName(sourceFile);

            new TorrentUpload().Log(
                $"Starting process for creating a torrent for the file. Filename: [{fileName}]",
                EventLogEntryType.Information);

            //Error Check the required values

            if (string.IsNullOrWhiteSpace(sourceFile) || !Directory.Exists(sourceFile) && !File.Exists(sourceFile))
                throw new ArgumentException(
                    $"Source for creating the torrent is not valid. Path: [{sourceFile}]");

            var attr = File.GetAttributes(sourceFile);

            var createTorrentsForFolders =
                Convert.ToBoolean(ConfigurationManager.AppSettings["CreateTorrentsForFolders"]);
            if (!createTorrentsForFolders)
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    throw new ArgumentException(
                        $"Source for creating the torrent is not valid, it's a directory. Path: [{sourceFile}]");

            var skipFiles = ConfigurationManager.AppSettings["SkipFiles"]
                .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).ToList();

            //Exit if file extension is one that is supposed to be skipped
            foreach (var fileType in skipFiles)
                if (Path.GetExtension(sourceFile) == fileType)
                    throw new ArgumentException(
                        $"Source for creating the torrent is not valid, it's a {fileType} extension. [{sourceFile}]");

            new TorrentUpload().Log(
                $"Queueing file to be processed later. Filename: [{fileName}]",
                EventLogEntryType.Information);
            new TorrentUpload().InsertOrUpdateDb(
                $"INSERT INTO tTorrentCreatorUploaderFiles (FilePath, Processed, CreatedDate) VALUES('{sourceFile.Replace("'", "''")}', False, NOW())");
            new TorrentUpload().Log(
                $"File queued successfully. Filename: [{fileName}]",
                EventLogEntryType.Information);
        }

        public void CreateTorrentUpload(string sourceFile, string fileName, TorrentCreatorUploaderFile queuedFileInfo)
        {
            var targetPath = ConfigurationManager.AppSettings["UploadFolder"];
            var destFile = CreateTorrentFile(sourceFile, fileName, queuedFileInfo, targetPath);

            var torrentFilePath = destFile.Replace(Path.GetFileName(destFile), Convert.ToString(queuedFileInfo.Guid)) +
                                  ".torrent";

            new TorrentUpload().InsertOrUpdateDb(
                $"UPDATE tTorrentCreatorUploaderFiles SET CreatedTorrent = True, CreatedTorrentFilePath = '{torrentFilePath}' WHERE ID = {queuedFileInfo.Id}");

            var uploadTorrent = Convert.ToBoolean(ConfigurationManager.AppSettings["UploadTorrent"]);
            var sendTorrentToUTorrentViaWebApi =
                Convert.ToBoolean(ConfigurationManager.AppSettings["SendTorrentToUTorrentViaWebApi"]);
            var sendTorrentToDelugeViaDelugeConsole =
                Convert.ToBoolean(ConfigurationManager.AppSettings["SendTorrentToDelugeViaDelugeConsole"]);
            var copyTorrentFile = Convert.ToBoolean(ConfigurationManager.AppSettings["CopyTorrentFile"]);
            var deleteTorrentFileAfterEveryThing =
                Convert.ToBoolean(ConfigurationManager.AppSettings["DeleteTorrentFileAfterEveryThing"]);

            if (queuedFileInfo.UploadedTorrent) uploadTorrent = false;
            if (uploadTorrent) UploadTorrent(sourceFile, fileName, queuedFileInfo, destFile);

            if (queuedFileInfo.SentTorrentToUTorrentViaWebApi) sendTorrentToUTorrentViaWebApi = false;
            if (sendTorrentToUTorrentViaWebApi) SendToUTorrent(fileName, queuedFileInfo, destFile);

            if (queuedFileInfo.SentTorrentToDelugeViaDelugeConsole) sendTorrentToDelugeViaDelugeConsole = false;
            if (sendTorrentToDelugeViaDelugeConsole) SendToDeluge(fileName, queuedFileInfo, destFile);

            var torrentFileName = Path.GetFileName(torrentFilePath);

            if (queuedFileInfo.CopiedTorrentFile) copyTorrentFile = false;
            if (copyTorrentFile) CopyTorrent(queuedFileInfo, targetPath, torrentFilePath, torrentFileName);

            if (queuedFileInfo.DeletedTorrentFileAfterEveryThing) deleteTorrentFileAfterEveryThing = false;
            if (deleteTorrentFileAfterEveryThing) DeleteTorrent(queuedFileInfo, torrentFileName, torrentFilePath);
        }

        public void DeleteTorrent(TorrentCreatorUploaderFile queuedFileInfo, string torrentFileName,
            string torrentFilePath)
        {
            new TorrentUpload().Log(
                $"Starting process of deleting .torrent file. Filename: [{torrentFileName}]",
                EventLogEntryType.Information);
            if (!File.Exists(torrentFilePath)) return;
            new TorrentUpload().Log(
                $"Deleting .torrent file. Filename: [{torrentFileName}]",
                EventLogEntryType.Information);
            File.Delete(torrentFilePath);

            new TorrentUpload().InsertOrUpdateDb(
                $"UPDATE tTorrentCreatorUploaderFiles SET DeletedTorrentFileAfterEveryThing = True WHERE ID = {queuedFileInfo.Id}");
        }

        public void CopyTorrent(TorrentCreatorUploaderFile queuedFileInfo, string targetPath, string torrentFilePath,
            string torrentFileName)
        {
            new TorrentUpload().Log(
                $"Starting process of copying .torrent file. Filename: [{torrentFileName}]",
                EventLogEntryType.Information);
            if (File.Exists(torrentFilePath))
            {
                new TorrentUpload().Log(
                    $"Copying .torrent file. Filename: [{torrentFileName}]",
                    EventLogEntryType.Information);
                var copyTorrentFilePath = ConfigurationManager.AppSettings["CopyTorrentFilePath"];
                if (!Directory.Exists(copyTorrentFilePath)) Directory.CreateDirectory(copyTorrentFilePath);
                var destTorrentFile = Path.Combine(copyTorrentFilePath,
                    torrentFileName);
                File.Copy(torrentFilePath, destTorrentFile, true);
                new TorrentUpload().Log($"File copied to upload folder. [Path: {targetPath}]",
                    EventLogEntryType.Information);

                new TorrentUpload().InsertOrUpdateDb(
                    $"UPDATE tTorrentCreatorUploaderFiles SET CopiedTorrentFile = True WHERE ID = {queuedFileInfo.Id}");
            }
            else
            {
                new TorrentUpload().Log(
                    $"Torrent file does not exist to copy. Path: [{torrentFilePath}]",
                    EventLogEntryType.Information);
            }
        }

        public void SendToDeluge(string fileName, TorrentCreatorUploaderFile queuedFileInfo, string destFile)
        {
            var delugeIp = ConfigurationManager.AppSettings["DelugeIP"];
            var delugePort = Convert.ToInt32(ConfigurationManager.AppSettings["DelugePort"]);
            var delugeUsername = ConfigurationManager.AppSettings["DelugeUsername"];
            var delugePassword = ConfigurationManager.AppSettings["DelugePassword"];
            var delugeInstalledFolder = ConfigurationManager.AppSettings["DelugeInstalledFolder"];

            new TorrentUpload().Log(
                $"Starting process of sending torrent via Deluge debug console. Filename: [{fileName}]",
                EventLogEntryType.Information);
            new TorrentUpload().SendTorrentToDelugeViaDelugeConsole(destFile, delugeIp, delugePort, delugeUsername,
                delugePassword, delugeInstalledFolder, queuedFileInfo.Guid);

            new TorrentUpload().InsertOrUpdateDb(
                $"UPDATE tTorrentCreatorUploaderFiles SET SentTorrentToDelugeViaDelugeConsole = True WHERE ID = {queuedFileInfo.Id}");
        }

        public void SendToUTorrent(string fileName, TorrentCreatorUploaderFile queuedFileInfo, string destFile)
        {
            var uTorrentIp = ConfigurationManager.AppSettings["uTorrentIP"];
            var uTorrentPort = Convert.ToInt32(ConfigurationManager.AppSettings["uTorrentPort"]);
            var uTorrentUsername = ConfigurationManager.AppSettings["uTorrentUsername"];
            var uTorrentPassword = ConfigurationManager.AppSettings["uTorrentPassword"];

            new TorrentUpload().Log(
                $"Starting process of sending torrent via uTorrent Web UI. Filename: [{fileName}]",
                EventLogEntryType.Information);
            new TorrentUpload().SendTorrentToUTorrentViaWebUi(destFile, uTorrentIp, uTorrentPort, uTorrentUsername,
                uTorrentPassword, queuedFileInfo.Guid);

            new TorrentUpload().InsertOrUpdateDb(
                $"UPDATE tTorrentCreatorUploaderFiles SET SentTorrentToUTorrentViaWebApi = True WHERE ID = {queuedFileInfo.Id}");
        }

        public void UploadTorrent(string sourceFile, string fileName, TorrentCreatorUploaderFile queuedFileInfo,
            string destFile)
        {
            var torrentSiteUrl = ConfigurationManager.AppSettings["TorrentSiteUrl"];
            var torrentSiteUsername = ConfigurationManager.AppSettings["TorrentSiteUsername"];
            var torrentSitePassword = ConfigurationManager.AppSettings["TorrentSitePassword"];
            var torrentSiteAccountLoginUrl = ConfigurationManager.AppSettings["TorrentSiteAccountLoginUrl"];
            var torrentSiteUploadPageUrl = ConfigurationManager.AppSettings["TorrentSiteUploadPageUrl"];
            var uploadTorrentDescription = ConfigurationManager.AppSettings["UploadTorrentDescription"];
            var uploadTorrentCategoryId = ConfigurationManager.AppSettings["UploadTorrentCategoryId"];
            var uploadTorrentLanguageId = ConfigurationManager.AppSettings["UploadTorrentLanguageId"];

            new TorrentUpload().Log(
                $"Starting process of uploading torrent to torrent site via RestSharp. Filename: [{fileName}]",
                EventLogEntryType.Information);
            new TorrentUpload().UploadTorrentViaRestSharp(destFile, sourceFile, torrentSiteUrl, torrentSiteUsername,
                torrentSitePassword, torrentSiteAccountLoginUrl,
                torrentSiteUploadPageUrl, uploadTorrentDescription, uploadTorrentCategoryId,
                uploadTorrentLanguageId, string.Empty, string.Empty, queuedFileInfo.Guid);

            new TorrentUpload().InsertOrUpdateDb(
                $"UPDATE tTorrentCreatorUploaderFiles SET UploadedTorrent = True WHERE ID = {queuedFileInfo.Id}");
        }

        public string CreateTorrentFile(string sourceFile, string fileName, TorrentCreatorUploaderFile queuedFileInfo,
            string targetPath)
        {
            string destFile;
            var copyFileToUploadFolder = Convert.ToBoolean(ConfigurationManager.AppSettings["CopyFileToUploadFolder"]);

            if (copyFileToUploadFolder)
            {
                new TorrentUpload().Log(
                    $"Starting process for copying file to upload folder. Filename: [{fileName}]",
                    EventLogEntryType.Information);
                // To copy a folder's contents to a new location:
                if (fileName != null)
                {
                    destFile = Path.Combine(targetPath, fileName);

                    // Create a new target folder, if necessary.
                    if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

                    new TorrentUpload().Log(
                        $"File to be copied to upload folder. [Path: {targetPath}]",
                        EventLogEntryType.Information);

                    var waitForFileToBeIdle =
                        Convert.ToBoolean(ConfigurationManager.AppSettings["WaitForFileToBeIdle"]);
                    if (waitForFileToBeIdle && new TorrentUpload().GetIdleFile(sourceFile))
                        new TorrentUpload().Log(
                            $"In process of copying file to upload folder, busy seeing if file is idle. Filename: [{fileName}]",
                            EventLogEntryType.Information);
                    if (waitForFileToBeIdle && new TorrentUpload().GetIdleFile(sourceFile))
                    {
                        new TorrentUpload().Log(
                            $"In process of copying file to upload folder, copying file now. Filename: [{fileName}]",
                            EventLogEntryType.Information);
                        // To copy a file to another location and overwrite the destination file if it already exists.
                        File.Copy(sourceFile, destFile, true);
                        new TorrentUpload().Log($"File copied to upload folder. [Path: {targetPath}]",
                            EventLogEntryType.Information);
                    }
                    else
                    {
                        new TorrentUpload().Log(
                            $"In process of copying file to upload folder, busy seeing if file is locked. Filename: [{fileName}]",
                            EventLogEntryType.Information);
                        if (new TorrentUpload().IsFileLocked(sourceFile))
                            throw new ArgumentException(
                                $"File for creating the torrent is locked for too long, skipping torrent create process. Filename: [{sourceFile}]");

                        new TorrentUpload().Log(
                            $"In process of copying file to upload folder, copying file now. Filename: [{fileName}]",
                            EventLogEntryType.Information);
                        // To copy a file to another location and overwrite the destination file if it already exists.
                        File.Copy(sourceFile, destFile, true);
                        new TorrentUpload().Log($"File copied to upload folder. [Path: {targetPath}]",
                            EventLogEntryType.Information);
                    }
                }
                else
                {
                    throw new ArgumentException(
                        $"Source for creating the torrent is not valid. Filename: [{sourceFile}]");
                }
            }
            else
            {
                destFile = sourceFile;
            }

            new TorrentUpload().Log(
                $"Starting actual process of creating torrent file. Filename: [{fileName}]",
                EventLogEntryType.Information);
            CreateTorrent(destFile, queuedFileInfo.Guid);
            return destFile;
        }

        private void CreateTorrent(string fullPath, Guid guid)
        {
            //Declare and set variables for torrent
            var announceList = ConfigurationManager.AppSettings["AnnounceList"]
                .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            var comment = ConfigurationManager.AppSettings["Comment"];
            var createdBy = ConfigurationManager.AppSettings["CreatedBy"];
            var publisher = ConfigurationManager.AppSettings["Publisher"];
            var publisherUrl = ConfigurationManager.AppSettings["PublisherUrl"];
            var pieceSize = Convert.ToInt32(ConfigurationManager.AppSettings["PieceSize"]);
            var isPrivate = Convert.ToBoolean(ConfigurationManager.AppSettings["Private"]);
            var storeMd5 = Convert.ToBoolean(ConfigurationManager.AppSettings["StoreMD5"]);
            var ignoreHidden = Convert.ToBoolean(ConfigurationManager.AppSettings["IgnoreHidden"]);

            //Create the torrent
            var torrCreator = new TorrentCreator();

            if (announceList.Count > 0)
                torrCreator.Announces.Add(announceList);
            if (!string.IsNullOrWhiteSpace(comment))
                torrCreator.Comment = comment;
            if (!string.IsNullOrWhiteSpace(createdBy))
                torrCreator.CreatedBy = createdBy;
            if (!string.IsNullOrWhiteSpace(publisher))
                torrCreator.Publisher = publisher;
            if (!string.IsNullOrWhiteSpace(publisherUrl))
                torrCreator.PublisherUrl = publisherUrl;
            if (pieceSize != 0)
                torrCreator.PieceLength = (long) Math.Pow(2, pieceSize + 13);
            torrCreator.Private = isPrivate;
            torrCreator.StoreMD5 = storeMd5;

            //torrCreator.BeginCreate(new NbTorrentFileSource(fullPath, ignoreHidden, String.Empty), TorrentCreated, torrCreator);
            torrCreator.Create(new TorrentFileSource(fullPath, ignoreHidden, string.Empty),
                fullPath.Replace(Path.GetFileName(fullPath), Convert.ToString(guid)) + ".torrent");
            new TorrentUpload().Log(
                $"Torrent {fullPath.Replace(Path.GetFileName(fullPath), Convert.ToString(guid)) + ".torrent"} created successfully for file {Path.GetFileName(fullPath)}.",
                EventLogEntryType.Information);
        }

        //private void TorrentCreated(IAsyncResult result)
        //{
        //    try
        //    {
        //        //Save the torrent file
        //        var torrentCreator = result.AsyncState as TorrentCreator;
        //        if (torrentCreator != null)
        //            torrentCreator.EndCreate(result, _fullPath + ".torrent");
        //        (new TorrentUpload()).Log(String.Format("Torrent {0} created successfully.", Path.GetFileName(_fullPath)), EventLogEntryType.Information);
        //        _fullPath = null;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
    }
}