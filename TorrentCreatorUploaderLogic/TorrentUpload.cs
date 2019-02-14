using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using RestSharp;
using UTorrent.Api;

namespace TorrentCreatorUploaderLogic
{
    public class TorrentUpload
    {
        public enum UploadTorrentCategory
        {
            [Description("3D Movies SD")] ThreeD_Movies_SD = 59,
            [Description("3D Movies SD Old")] ThreeD_Movies_SD_Old = 60,
            [Description("3D Movies HD")] ThreeD_Movies_HD = 61,
            [Description("3D Movies HD Old")] ThreeD_Movies_HD_Old = 62,
            [Description("4K 4K Images")] FourK_FourK_Images = 77,
            [Description("4K 4K Other")] FourK_FourK_Other = 78,
            [Description("4K 4K KidsCartoon")] FourK_FourK_Kids_Cartoon = 80,
            [Description("4K 4K Anime")] FourK_FourK_Anime = 81,
            [Description("4K 4K Sport")] FourK_FourK_Sport = 82,
            [Description("4K 4K Documentaries")] FourK_FourK_Documentaries = 83,
            [Description("4K 4K S.A Movies")] FourK_FourK_SA_Movies = 84,
            [Description("4K 4K HD Ultra Movies")] FourK_FourK_HD_Ultra_Movies = 85,
            [Description("4K 4K Music Videos")] FourK_FourK_Music_Videos = 86,
            [Description("4K 4K Trailers")] FourK_FourK_Trailers = 87,
            [Description("4K 4K Apps")] FourK_FourK_Apps = 88,
            [Description("4K 4K 3D Ultra Movies")] FourK_FourK_ThreeD_Ultra_Movies = 89,
            [Description("4K 4K TV Series")] FourK_FourK_TV_Series = 90,
            [Description("Apps Linux")] Apps_Linux = 20,
            [Description("Apps Linux")] Apps_Mac = 19,
            [Description("Apps PC")] Apps_PC = 18,
            [Description("Apps Other")] Apps_Other = 21,
            [Description("Games Mac")] Games_Mac = 12,
            [Description("Games PC")] Games_PC = 10,
            [Description("Games PS")] Games_PS = 11,
            [Description("Games Wii")] Games_Wii = 44,
            [Description("Games XBox")] Games_XBox = 13,
            [Description("Games XBox Kinect")] Games_XBox_Kinect = 16,
            [Description("Games Other")] Games_Other = 17,
            [Description("Games PSP")] Games_PSP = 92,
            [Description("Movies Anime")] Movies_Anime = 1,
            [Description("Movies CamTS")] Movies_Cam_TS = 22,
            [Description("Movies DVD")] Movies_DVD = 2,
            [Description("Movies KidsCartoons")] Movies_Kids_Cartoons = 23,
            [Description("Movies HD-Movies")] Movies_HD_Movies = 42,
            [Description("Movies SD-Movies")] Movies_SD_Movies = 3,
            [Description("Movies Trailers")] Movies_Trailers = 72,
            [Description("Movies Other")] Movies_Other = 4,
            [Description("Music MP3")] Music_MPThree = 24,
            [Description("Music DVD")] Music_DVD = 25,
            [Description("Music Video")] Music_Video = 26,
            [Description("Music HD VideoAudio")] Music_HD_Video_Audio = 27,
            [Description("Old Stuff Movies")] Old_Stuff_Movies = 35,
            [Description("Old Stuff Games")] Old_Stuff_Games = 36,
            [Description("Old Stuff Apps")] Old_Stuff_Apps = 37,
            [Description("Old Stuff Other")] Old_Stuff_Other = 38,
            [Description("Old Stuff Music")] Old_Stuff_Music = 39,
            [Description("Old Stuff TV")] Old_Stuff_TV = 40,
            [Description("Old Stuff Anime")] Old_Stuff_Anime = 91,
            [Description("Other E-Books")] Other_EBooks = 47,
            [Description("Other Images")] Other_Images = 48,
            [Description("Other Mobile Phone")] Other_Mobile_Phone = 49,
            [Description("Other Other")] Other_Other = 50,
            [Description("Other Clips")] Other_Clips = 58,
            [Description("SA Stuff SA Movies")] SA_Stuff_SA_Movies = 64,
            [Description("SA Stuff SA Series")] SA_Stuff_SA_Series = 65,
            [Description("SA Stuff SA Music")] SA_Stuff_SA_Music = 66,
            [Description("SA Stuff SA Music HD")] SA_Stuff_SA_Music_HD = 70,
            [Description("SA Stuff SA Other")] SA_Stuff_SA_Other = 67,
            [Description("SA Stuff SA Movies HD")] SA_Stuff_SA_Movies_HD = 69,
            [Description("TV HD-Series")] TV_HD_Series = 41,
            [Description("TV SD-Series")] TV_SD_Series = 6,
            [Description("TV HD-Documentaries")] TV_HD_Documentaries = 7,
            [Description("TV SD-Documentaries")] TV_SD_Documentaries = 5,
            [Description("TV SD-Sports")] TV_SD_Sports = 34,
            [Description("TV HD-Sports")] TV_HD_Sports = 33,
            [Description("TV Other")] TV_Other = 57,
            [Description("TV Anime")] TV_Anime = 63,
            [Description("TV KidsCartoons")] TV_Kids_Cartoons = 74,
            [Description("TV Stand-Up Comedy")] TV_StandUp_Comedy = 73,
            [Description("TV Animation - Mature")] TV_Animation_Mature = 68
        }

        public enum UploadTorrentLanguage
        {
            [Description("Unknown/NA")] UnknownNA = 0,
            [Description("English")] English = 1,
            [Description("Afrikaans")] Afrikaans = 2,
            [Description("Other")] Other = 3
        }

        public void Log(string text, EventLogEntryType msgType)
        {
            //string logPath;
            //string logFolder = String.Format(@"{0}\logs\", AppDomain.CurrentDomain.BaseDirectory);
            //if (!File.Exists(logFolder))
            //{
            //    Directory.CreateDirectory(logFolder);
            //}
            //logPath = String.Format(@"{0}\log_{1}.txt", logFolder, DateTime.Now.ToString("yyyyMMdd"));
            //File.AppendAllText(logPath, String.Format("{0} | Torrent Create Uploader | {1}{2}", DateTime.Now, text, Environment.NewLine));
            //using (var eventLog = new EventLog("Application"))
            //{
            //    eventLog.Source = "Torrent Creator Uploader";
            //    eventLog.WriteEntry(text, msgType);
            //}
            InsertOrUpdateDb(
                $"INSERT INTO tTorrentCreatorUploaderLog (Type, Message, CreatedDate) VALUES('{msgType.ToString()}', '{text.Replace("'", "''")}', NOW())");

            var accessToken = ConfigurationManager.AppSettings["TelegramAccessToken"];
            var sendTelegramErrorMessages =
                Convert.ToBoolean(ConfigurationManager.AppSettings["SendTelegramErrorMessages"]);
            var sendTelegramInformationMessages =
                Convert.ToBoolean(ConfigurationManager.AppSettings["SendTelegramInformationMessages"]);

            switch (msgType)
            {
                case EventLogEntryType.Error when sendTelegramErrorMessages:
                    if (!text.Contains("Source for creating the torrent is not valid, it's a "))
                        new TelegramUtil().SendTelegramMsg(accessToken, text);
                    break;
                case EventLogEntryType.Information when sendTelegramInformationMessages:
                    new TelegramUtil().SendTelegramMsg(accessToken, text);
                    break;
            }
        }

        public bool IsFileLocked(string path)
        {
            FileStream stream = null;

            try
            {
                stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }

        public bool GetIdleFile(string path)
        {
            var fileIdle = false;
            var maximumAttemptsAllowed =
                Convert.ToInt32(ConfigurationManager.AppSettings["MaximumAttemptsAllowedWhenFileLocked"]);
            var threadSleepTimeInMillisecondsWhenLocked =
                Convert.ToInt32(ConfigurationManager.AppSettings["ThreadSleepTimeInMillisecondsWhenLocked"]);
            var attemptsMade = 0;

            while (!fileIdle && attemptsMade <= maximumAttemptsAllowed)
                try
                {
                    using (File.Open(path, FileMode.Open, FileAccess.ReadWrite))
                    {
                        fileIdle = true;
                    }
                }
                catch
                {
                    attemptsMade++;
                    Thread.Sleep(threadSleepTimeInMillisecondsWhenLocked);
                }

            return fileIdle;
        }

        public bool IsPathDirectory(string path)
        {
            var attr = File.GetAttributes(path);

            return attr.HasFlag(FileAttributes.Directory);
        }

        public List<TorrentCreatorUploaderFile> GetDb(string commandText)
        {
            var cnon = new OleDbConnection
            {
                ConnectionString =
                    $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={
                            AppDomain.CurrentDomain.BaseDirectory
                        }TorrentCreatorUploaderDB.accdb"
            };
            var command = new OleDbCommand {CommandText = commandText};
            cnon.Open();
            command.Connection = cnon;
            var selecteData = command.ExecuteReader();

            var fileList = new List<TorrentCreatorUploaderFile>();
            while (selecteData != null && selecteData.Read())
            {
                var data = new TorrentCreatorUploaderFile
                {
                    Id = (int) selecteData["Id"],
                    FilePath = (string) selecteData["FilePath"],
                    Guid = (Guid) selecteData["Guid"],
                    CreatedTorrent = (bool) selecteData["CreatedTorrent"],
                    UploadedTorrent = (bool) selecteData["UploadedTorrent"],
                    SentTorrentToUTorrentViaWebApi = (bool) selecteData["SentTorrentToUTorrentViaWebApi"],
                    SentTorrentToDelugeViaDelugeConsole = (bool) selecteData["SentTorrentToDelugeViaDelugeConsole"],
                    CopiedTorrentFile = (bool) selecteData["CopiedTorrentFile"],
                    DeletedTorrentFileAfterEveryThing = (bool) selecteData["DeletedTorrentFileAfterEveryThing"],
                    Processed = (bool) selecteData["Processed"]
                };
                if (selecteData["ProcessedDate"] != DBNull.Value)
                    data.ProcessedDate = (DateTime) selecteData["ProcessedDate"];
                data.Attempts = (int) selecteData["Attempts"];
                if (selecteData["ErrorMessage"] != DBNull.Value)
                    data.ErrorMessage = (string) selecteData["ErrorMessage"];
                if (selecteData["CreatedTorrentFilePath"] != DBNull.Value)
                    data.CreatedTorrentFilePath = (string) selecteData["CreatedTorrentFilePath"];
                data.CreatedDate = (DateTime) selecteData["CreatedDate"];
                fileList.Add(data);
            }

            cnon.Close();
            return fileList;
        }

        public List<TorrentCreatorUploaderLog> GetDbLogs(string commandText)
        {
            var cnon = new OleDbConnection
            {
                ConnectionString =
                    $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={
                            AppDomain.CurrentDomain.BaseDirectory
                        }TorrentCreatorUploaderDB.accdb"
            };
            var command = new OleDbCommand {CommandText = commandText};
            cnon.Open();
            command.Connection = cnon;
            var selecteData = command.ExecuteReader();

            var fileList = new List<TorrentCreatorUploaderLog>();
            while (selecteData != null && selecteData.Read())
            {
                var data = new TorrentCreatorUploaderLog
                {
                    Id = (int) selecteData["Id"],
                    Message = (string) selecteData["Message"],
                    CreatedDate = (DateTime) selecteData["CreatedDate"],
                    Type = (string) selecteData["Type"]
                };
                fileList.Add(data);
            }

            cnon.Close();
            return fileList;
        }

        public void InsertOrUpdateDb(string commandText)
        {
            var cnon = new OleDbConnection
            {
                ConnectionString =
                    $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={
                            AppDomain.CurrentDomain.BaseDirectory
                        }TorrentCreatorUploaderDB.accdb"
            };
            var command = new OleDbCommand {CommandText = commandText};
            cnon.Open();
            command.Connection = cnon;
            command.ExecuteNonQuery();
            cnon.Close();
        }

        public void UploadTorrentViaRestSharp(string filePath, string sourceFilePath, string torrentSiteUrl,
            string torrentSiteUsername, string torrentSitePassword,
            string torrentSiteAccountLoginUrl, string torrentSiteUploadPageUrl, string uploadTorrentDescription,
            string uploadTorrentCategoryId, string uploadTorrentLanguageId, string uploadTorrentNfoFile,
            string uploadTorrentImageFile, Guid guid)
        {
            var torrentFilePath = filePath.Replace(Path.GetFileName(filePath), Convert.ToString(guid)) + ".torrent";
            var folderCategoryId = 0;
            try
            {
                var category = GetValueFromDescription<UploadTorrentCategory>(Directory.GetParent(sourceFilePath).Name);
                folderCategoryId = Convert.ToInt32(category);
            }
            catch
            {
            }

            if (folderCategoryId != 0) uploadTorrentCategoryId = Convert.ToString(folderCategoryId);

            var cookieContainer = new CookieContainer();

            var client = new RestClient($"{torrentSiteUrl}") {CookieContainer = cookieContainer};

            var loginRequest = new RestRequest(torrentSiteAccountLoginUrl, Method.POST);
            loginRequest.AddParameter("username", torrentSiteUsername);
            loginRequest.AddParameter("password", torrentSitePassword);
            Log(
                $"Torrent upload process: In process of uploading torrent to torrent site via RestSharp, about to Login. Site: [{torrentSiteUrl + torrentSiteAccountLoginUrl}]",
                EventLogEntryType.Information);
            var loginResponse = client.Execute(loginRequest);
            if (Convert.ToInt32(loginResponse.StatusCode) == 0)
                throw new ArgumentException(
                    $"Torrent upload process: Failed uploading torrent to torrent site via RestSharp, was in Login Request process. Error: {loginResponse.ErrorMessage}. Site: [{torrentSiteUrl + torrentSiteAccountLoginUrl}]");
            //Log(String.Format("Torrent upload process: 'Account Login Page' Response Content: {0}.", loginResponse.Content), EventLogEntryType.Information);
            Log(
                $"Torrent upload process: Login returned response - [{Environment.NewLine}] [{loginResponse.Content}] [{Environment.NewLine}]Site: [{torrentSiteUrl + torrentSiteAccountLoginUrl}]",
                EventLogEntryType.Information);
            Log(
                $"Torrent upload process: In process of uploading torrent to torrent site via RestSharp, done making request for Login. Site: [{torrentSiteUrl + torrentSiteAccountLoginUrl}]",
                EventLogEntryType.Information);

            var uploadRequest = new RestRequest(torrentSiteUploadPageUrl, Method.POST);
            uploadRequest.AddParameter("takeupload", "yes");
            uploadRequest.AddFile("torrent", torrentFilePath);
            if (string.IsNullOrWhiteSpace(uploadTorrentNfoFile))
            {
                var nfoSourceFilePath = IsPathDirectory(sourceFilePath)
                    ? $"{sourceFilePath}.nfo"
                    : sourceFilePath.Replace(Path.GetExtension(sourceFilePath), ".nfo");
                string nfoCreatedTorrentPath;
                if (IsPathDirectory(filePath))
                    nfoCreatedTorrentPath = $"{filePath}.nfo";
                else
                    nfoCreatedTorrentPath =
                        filePath.Replace(Path.GetExtension(filePath),
                            ".nfo");
                var nfoFilePath = string.Empty;

                if (File.Exists(nfoSourceFilePath))
                {
                    var nfoText = File.ReadAllText(nfoSourceFilePath);
                    if (!nfoText.Contains("<plot />")) nfoFilePath = nfoSourceFilePath;
                }
                else if (File.Exists(nfoCreatedTorrentPath))
                {
                    var nfoText = File.ReadAllText(nfoCreatedTorrentPath);
                    if (!nfoText.Contains("<plot />")) nfoFilePath = nfoCreatedTorrentPath;
                }

                if (string.IsNullOrWhiteSpace(nfoFilePath))
                {
                    var directoryInfo = Directory.GetParent(sourceFilePath).Parent;
                    if (directoryInfo != null)
                    {
                        var nfoParent = $"{directoryInfo.FullName}\\tvshow.nfo";
                        if (File.Exists(nfoParent)) nfoFilePath = nfoParent;
                    }
                }

                if (!string.IsNullOrWhiteSpace(nfoFilePath) && File.Exists(nfoFilePath))
                    uploadRequest.AddFile("nfo", nfoFilePath);
                else
                    uploadRequest.AddParameter("nfo", "");
            }
            else
            {
                if (File.Exists(uploadTorrentNfoFile))
                    uploadRequest.AddFile("nfo", uploadTorrentNfoFile);
                else
                    uploadRequest.AddParameter("nfo", "");
            }

            if (Path.GetFileName(filePath).Replace(Path.GetExtension(filePath), "") == Convert.ToString(guid))
                uploadRequest.AddParameter("name", Path.GetFileName(sourceFilePath));
            else
                uploadRequest.AddParameter("name", Path.GetFileName(filePath));
            uploadRequest.AddParameter("imdb", "");
            uploadRequest.AddParameter("tube", "");
            if (string.IsNullOrWhiteSpace(uploadTorrentImageFile))
            {
                var imageSourceFilePath = IsPathDirectory(sourceFilePath)
                    ? $"{sourceFilePath}.jpg"
                    : sourceFilePath.Replace(Path.GetExtension(sourceFilePath), ".jpg");
                string imageCreatedTorrentPath;
                if (IsPathDirectory(filePath))
                    imageCreatedTorrentPath = $"{filePath}.jpg";
                else
                    imageCreatedTorrentPath =
                        filePath.Replace(Path.GetExtension(filePath),
                            ".jpg");
                var imageThumbSourceFilePath = IsPathDirectory(sourceFilePath)
                    ? $"{sourceFilePath}.jpg"
                    : sourceFilePath.Replace(Path.GetExtension(sourceFilePath), "-thumb.jpg");
                string imageThumbCreatedTorrentPath;
                if (IsPathDirectory(filePath))
                    imageThumbCreatedTorrentPath = $"{filePath}.jpg";
                else
                    imageThumbCreatedTorrentPath =
                        filePath.Replace(Path.GetExtension(filePath),
                            "-thumb.jpg");
                var imageFile = string.Empty;

                if (File.Exists(imageSourceFilePath))
                    imageFile = imageSourceFilePath;
                else if (File.Exists(imageCreatedTorrentPath))
                    imageFile = imageCreatedTorrentPath;
                else if (File.Exists(imageThumbSourceFilePath))
                    imageFile = imageThumbSourceFilePath;
                else if (File.Exists(imageThumbCreatedTorrentPath)) imageFile = imageThumbCreatedTorrentPath;

                if (string.IsNullOrWhiteSpace(imageFile))
                    for (var i = 100 - 1; i >= 0; i--)
                    {
                        var directoryInfo = Directory.GetParent(sourceFilePath).Parent;
                        if (directoryInfo == null) continue;
                        var imageParent =
                            $"{directoryInfo.FullName}\\season{i}-poster.jpg";
                        if (i < 10)
                            imageParent =
                                $"{directoryInfo.FullName}\\season0{i}-poster.jpg";
                        if (!File.Exists(imageParent)) continue;
                        imageFile = imageParent;
                        break;
                    }

                if (string.IsNullOrWhiteSpace(imageFile))
                {
                    var directoryInfo = Directory.GetParent(sourceFilePath).Parent;
                    if (directoryInfo != null)
                    {
                        var imageParent = $"{directoryInfo.FullName}\\poster.jpg";
                        if (File.Exists(imageParent)) imageFile = imageParent;
                    }
                }

                if (!string.IsNullOrWhiteSpace(imageFile) && File.Exists(imageFile))
                    uploadRequest.AddFile("image0", imageFile);
                else
                    uploadRequest.AddParameter("image0", "");
            }
            else
            {
                if (File.Exists(uploadTorrentImageFile))
                    uploadRequest.AddFile("image0", uploadTorrentImageFile);
                else
                    uploadRequest.AddParameter("image0", "");
            }

            uploadRequest.AddParameter("image1", "");
            uploadRequest.AddParameter("type", uploadTorrentCategoryId);
            uploadRequest.AddParameter("lang", uploadTorrentLanguageId);
            uploadRequest.AddParameter("descr", uploadTorrentDescription);
            uploadRequest.AddParameter("press", "Upload Torrent");

            Log(
                $"Torrent upload process: In process of uploading torrent to torrent site via RestSharp, about to upload file. Site: [{torrentSiteUrl + torrentSiteUploadPageUrl}]",
                EventLogEntryType.Information);
            var uploadResponse = client.Execute(uploadRequest);
            if (Convert.ToInt32(uploadResponse.StatusCode) == 0)
            {
                Log(
                    $"Torrent upload process: Upload returned response - [{Environment.NewLine}] [{uploadResponse.Content}] [{Environment.NewLine}]Site: [{torrentSiteUrl + torrentSiteUploadPageUrl}]",
                    EventLogEntryType.Information);
                throw new ArgumentException(
                    $"Torrent upload process: Failed uploading torrent to torrent site via RestSharp, was in Upload Request process. Error:{uploadResponse.ErrorMessage}. Site: [{torrentSiteUrl + torrentSiteUploadPageUrl}]");
            }

            if (uploadResponse.Content.Contains("Torrent already uploaded"))
            {
                Log(
                    $"Torrent upload process: Torrent was uploaded already, marking as uploaded - [{Environment.NewLine}] [{uploadResponse.Content}] [{Environment.NewLine}]Site: [{torrentSiteUrl + torrentSiteUploadPageUrl}]",
                    EventLogEntryType.Information);
            }
            else if (!uploadResponse.Content.Contains("Torrent Uploaded OK"))
            {
                Log(
                    $"Torrent upload process: Upload returned response - [{Environment.NewLine}] [{uploadResponse.Content}] [{Environment.NewLine}]Site: [{torrentSiteUrl + torrentSiteUploadPageUrl}]",
                    EventLogEntryType.Information);
                throw new ArgumentException(
                    $"Torrent upload process: Failed uploading torrent to torrent site via RestSharp, was in Upload Request process, did not succeed in the upload. Site: [{torrentSiteUrl + torrentSiteUploadPageUrl}]");
            }

            //Log(String.Format("Torrent upload process: 'Upload Page' Response Content: {0}.", uploadResponse.Content), EventLogEntryType.Information);
            Log(
                $"Torrent upload process: Upload returned response - [{Environment.NewLine}] [{uploadResponse.Content}] [{Environment.NewLine}]Site: [{torrentSiteUrl + torrentSiteUploadPageUrl}]",
                EventLogEntryType.Information);
            Log(
                $"Torrent upload process: In process of uploading torrent to torrent site via RestSharp, done making request for Upload Torrent. Site: [{torrentSiteUrl + torrentSiteUploadPageUrl}]",
                EventLogEntryType.Information);
        }

        public void SendTorrentToUTorrentViaWebUi(string filePath, string uTorrentIp, int uTorrentPort,
            string uTorrentUsername, string uTorrentPassword, Guid guid)
        {
            var torrentFilePath = filePath.Replace(Path.GetFileName(filePath), Convert.ToString(guid)) + ".torrent";
            var fs = new FileStream(torrentFilePath, FileMode.Open);
            var client = new UTorrentClient(uTorrentIp, uTorrentPort, uTorrentUsername, uTorrentPassword);
            Log(
                $"Torrent SendTorrentToUTorrentViaWebUi process: about to send torrent file to uTorrent. URL: [{uTorrentIp}:{uTorrentPort}]",
                EventLogEntryType.Information);

            var clientSettings = client.GetSettings();
            var dirCompletedDownloadFlag = false;
            var dirCompletedDownload = "";

            foreach (var item in clientSettings.Result.Source["settings"])
            {
                if (item[0].ToString() == "dir_completed_download_flag")
                {
                    dirCompletedDownloadFlag = Convert.ToBoolean(item[2].ToString());
                }
                if (item[0].ToString() == "dir_completed_download")
                {
                    dirCompletedDownload = item[2].ToString();
                }
            }

            client.SetSetting("dir_completed_download_flag", true);
            client.SetSetting("dir_completed_download", Path.GetDirectoryName(filePath));
            Log(
                $"Torrent SendTorrentToUTorrentViaWebUi process: updating uTorrent settings to the file path. Path: {Path.GetDirectoryName(filePath)}",
                EventLogEntryType.Information);
            var response = client.PostTorrent(fs);
            var torrent = response.AddedTorrent;
            client.SetSetting("dir_completed_download_flag", dirCompletedDownloadFlag);
            client.SetSetting("dir_completed_download", dirCompletedDownload);
            Log(
                $"Torrent SendTorrentToUTorrentViaWebUi process: reverting uTorrent settings to original path settings. Path: {dirCompletedDownload} ",
                EventLogEntryType.Information);

            Log(
                $"Torrent {Path.GetFileName(filePath)} sent to uTorrent successfully via Web UI.",
                EventLogEntryType.Information);
        }

        public void SendTorrentToDelugeViaDelugeConsole(string filePath, string delugeIp, int delugePort,
            string uTorrentUsername, string uTorrentPassword, string delugeInstalledFolder, Guid guid)
        {
            var torrentFilePath = filePath.Replace(Path.GetFileName(filePath), Convert.ToString(guid)) + ".torrent";
            Log(
                "Torrent SendTorrentToDelugeViaDelugeConsole process: about to send torrent file to Deluge, setting command prompt.",
                EventLogEntryType.Information);
            var cmd = new Process
            {
                StartInfo =
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            cmd.Start();

            Log(
                "Torrent SendTorrentToDelugeViaDelugeConsole process: about to send torrent file to Deluge, setting commands to send .torrent file.",
                EventLogEntryType.Information);
            cmd.StandardInput.WriteLine("cd\\");
            cmd.StandardInput.WriteLine(Path.GetPathRoot(torrentFilePath)?.Replace("\\", string.Empty) + ":");
            cmd.StandardInput.WriteLine("cd {0}", delugeInstalledFolder);
            cmd.StandardInput.WriteLine("deluge-console connect {0}:{1} {2} {3}; add -p '{4}' '{5}';", delugeIp,
                delugePort, uTorrentUsername, uTorrentPassword, Path.GetDirectoryName(torrentFilePath),
                torrentFilePath);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            var cmdReturnMsg = cmd.StandardOutput.ReadToEnd();
            if (cmdReturnMsg.Contains("doesn't exist!"))
                throw new ArgumentException(
                    $"Torrent SendTorrentToDelugeViaDelugeConsole process: Failed to send torrent to Deluge, does not exist error occurred. File: [{filePath}]");
            if (!cmdReturnMsg.Contains("Torrent added!"))
                throw new ArgumentException(
                    $"Torrent SendTorrentToDelugeViaDelugeConsole process: Failed to send torrent to Deluge.{Environment.NewLine}{cmdReturnMsg} File: [{filePath}]");
            Log(cmdReturnMsg, EventLogEntryType.Information);
            Log(
                $"Torrent {Path.GetFileName(filePath)} sent to Deluge successfully via Deluge Console.",
                EventLogEntryType.Information);
        }

        public string ProcessQueuedFile(int fileId)
        {
            new TorrentUpload().Log($"Starting to do full process for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            var fileList = new TorrentUpload().GetDb(
                $"SELECT * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND ID = {fileId}");

            if (fileList.Count == 0)
                throw new ArgumentException(
                    $"File Id {fileId} not available to complete action, 'Processed' must be set to 'True'.");

            var fileName = string.Empty;
            foreach (var file in fileList)
                try
                {
                    var sourceFile = file.FilePath;
                    fileName = Path.GetFileName(sourceFile);

                    if (file.Processed)
                        throw new ArgumentException($"File '{fileName}' already marked as processed");

                    new TorrentUpload().Log($"Full processing for queued file. Filename: [{fileName}]",
                        EventLogEntryType.Information);
                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET Attempts = {file.Attempts + 1} WHERE ID = {file.Id}");

                    new TorrentCreate().CreateTorrentUpload(sourceFile, fileName, file);

                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET Processed = True, ProcessedDate = NOW() WHERE ID = {file.Id}");
                    new TorrentUpload().Log(
                        $"Queued file fully processed successfully. Filename: [{fileName}]",
                        EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    new TorrentUpload().Log(
                        $"Queued file not fully processed successfully, check accdb for error message. Filename: [{file.FilePath}]",
                        EventLogEntryType.Information);
                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET ErrorMessage = '{errorMessage.Replace("'", "''")}' WHERE ID = {file.Id}");
                    throw;
                }

            new TorrentUpload().Log($"Completed fully processing queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            return fileName;
        }

        public string CreateTorrentQueuedFile(int fileId)
        {
            new TorrentUpload().Log($"Starting to create torrent file for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            var fileList = new TorrentUpload().GetDb(
                $"SELECT * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND ID = {fileId}");

            if (fileList.Count == 0)
                throw new ArgumentException(
                    $"File Id {fileId} not available to complete action, 'Processed' must be set to 'True'.");

            var fileName = string.Empty;
            foreach (var file in fileList)
                try
                {
                    var sourceFile = file.FilePath;
                    fileName = Path.GetFileName(sourceFile);
                    if (file.CreatedTorrent)
                        throw new ArgumentException($"Torrent file already created for '{fileName}'");
                    new TorrentUpload().Log($"Creating torrent for queued file. Filename: [{fileName}]",
                        EventLogEntryType.Information);

                    var targetPath = ConfigurationManager.AppSettings["UploadFolder"];
                    var destFile = new TorrentCreate().CreateTorrentFile(sourceFile, fileName, file, targetPath);

                    var torrentFilePath = destFile.Replace(Path.GetFileName(destFile), Convert.ToString(file.Guid)) +
                                          ".torrent";

                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET CreatedTorrent = True, CreatedTorrentFilePath = '{torrentFilePath}', ProcessedDate = NOW() WHERE ID = {file.Id}");
                    new TorrentUpload().Log(
                        $"Created torrent file successfully. Filename: [{fileName}]",
                        EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    new TorrentUpload().Log(
                        $"Creating torrent process not successful, check accdb for error message. Filename: [{file.FilePath}]",
                        EventLogEntryType.Information);
                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET ErrorMessage = '{errorMessage.Replace("'", "''")}' WHERE ID = {file.Id}");
                    throw;
                }

            new TorrentUpload().Log($"Completed creating torrent for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            return fileName;
        }

        public string UploadTorrentQueuedFile(int fileId)
        {
            new TorrentUpload().Log($"Starting to upload torrent file for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            var fileList = new TorrentUpload().GetDb(
                $"SELECT * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND ID = {fileId}");

            if (fileList.Count == 0)
                throw new ArgumentException(
                    $"File Id {fileId} not available to complete action, 'Processed' must be set to 'True'.");

            var fileName = string.Empty;
            foreach (var file in fileList)
                try
                {
                    var sourceFile = file.FilePath;
                    fileName = Path.GetFileName(sourceFile);
                    if (!file.CreatedTorrent)
                        throw new ArgumentException(
                            $"The torrent file is not created for '{fileName}' to upload to the website");
                    if (file.UploadedTorrent)
                        throw new ArgumentException($"Torrent file already uploaded for '{fileName}'");
                    new TorrentUpload().Log($"Uploading torrent for queued file. Filename: [{fileName}]",
                        EventLogEntryType.Information);

                    new TorrentCreate().UploadTorrent(sourceFile, fileName, file, file.CreatedTorrentFilePath);

                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET UploadedTorrent = True, ProcessedDate = NOW() WHERE ID = {file.Id}");
                    new TorrentUpload().Log(
                        $"Uploaded torrent file successfully. Filename: [{fileName}]",
                        EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    new TorrentUpload().Log(
                        $"Uploading torrent file process not successful, check accdb for error message. Filename: [{file.FilePath}]",
                        EventLogEntryType.Information);
                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET ErrorMessage = '{errorMessage.Replace("'", "''")}' WHERE ID = {file.Id}");
                    throw;
                }

            new TorrentUpload().Log($"Completed uploading torrent to website for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            return fileName;
        }

        public string UTorrentUploadTorrentQueuedFile(int fileId)
        {
            new TorrentUpload().Log($"Starting to send torrent file to uTorrent for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            var fileList = new TorrentUpload().GetDb(
                $"SELECT * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND ID = {fileId}");

            if (fileList.Count == 0)
                throw new ArgumentException(
                    $"File Id {fileId} not available to complete action, 'Processed' must be set to 'True'.");

            var fileName = string.Empty;
            foreach (var file in fileList)
                try
                {
                    var sourceFile = file.FilePath;
                    fileName = Path.GetFileName(sourceFile);
                    if (!file.CreatedTorrent)
                        throw new ArgumentException(
                            $"The torrent file is not created for '{fileName}' to send to uTorrent");
                    if (file.SentTorrentToUTorrentViaWebApi)
                        throw new ArgumentException($"Torrent file already sent to uTorrent for '{fileName}'");
                    new TorrentUpload().Log($"Sending torrent to uTorrent for queued file. Filename: [{fileName}]",
                        EventLogEntryType.Information);

                    new TorrentCreate().SendToUTorrent(fileName, file, file.CreatedTorrentFilePath);

                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET SentTorrentToUTorrentViaWebApi = True, ProcessedDate = NOW() WHERE ID = {file.Id}");
                    new TorrentUpload().Log(
                        $"Sent torrent file to uTorrent successfully. Filename: [{fileName}]",
                        EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    new TorrentUpload().Log(
                        $"Sending torrent file to uTorrent process not successful, check accdb for error message. Filename: [{file.FilePath}]",
                        EventLogEntryType.Information);
                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET ErrorMessage = '{errorMessage.Replace("'", "''")}' WHERE ID = {file.Id}");
                    throw;
                }

            new TorrentUpload().Log($"Completed sending torrent file to uTorrent for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            return fileName;
        }

        public string DelugeUploadTorrentQueuedFile(int fileId)
        {
            new TorrentUpload().Log($"Starting to send torrent file to Deluge for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            var fileList = new TorrentUpload().GetDb(
                $"SELECT * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND ID = {fileId}");

            if (fileList.Count == 0)
                throw new ArgumentException(
                    $"File Id {fileId} not available to complete action, 'Processed' must be set to 'True'.");

            var fileName = string.Empty;
            foreach (var file in fileList)
                try
                {
                    var sourceFile = file.FilePath;
                    fileName = Path.GetFileName(sourceFile);
                    if (!file.CreatedTorrent)
                        throw new ArgumentException(
                            $"The torrent file is not created for '{fileName}' to send to Deluge");
                    if (file.SentTorrentToDelugeViaDelugeConsole)
                        throw new ArgumentException($"Torrent file already sent to Deluge for '{fileName}'");
                    new TorrentUpload().Log($"Sending torrent to Deluge for queued file. Filename: [{fileName}]",
                        EventLogEntryType.Information);

                    new TorrentCreate().SendToDeluge(fileName, file, file.CreatedTorrentFilePath);

                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET SentTorrentToDelugeViaDelugeConsole = True, ProcessedDate = NOW() WHERE ID = {file.Id}");
                    new TorrentUpload().Log(
                        $"Sent torrent file to Deluge successfully. Filename: [{fileName}]",
                        EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    new TorrentUpload().Log(
                        $"Sending torrent file to Deluge process not successful, check accdb for error message. Filename: [{file.FilePath}]",
                        EventLogEntryType.Information);
                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET ErrorMessage = '{errorMessage.Replace("'", "''")}' WHERE ID = {file.Id}");
                    throw;
                }

            new TorrentUpload().Log($"Completed sending torrent file to Deluge for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            return fileName;
        }

        public string CopyTorrentQueuedFile(int fileId)
        {
            new TorrentUpload().Log($"Starting to copy torrent file for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            var fileList = new TorrentUpload().GetDb(
                $"SELECT * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND ID = {fileId}");

            if (fileList.Count == 0)
                throw new ArgumentException(
                    $"File Id {fileId} not available to complete action, 'Processed' must be set to 'True'.");

            var fileName = string.Empty;
            foreach (var file in fileList)
                try
                {
                    var sourceFile = file.FilePath;
                    fileName = Path.GetFileName(sourceFile);
                    if (!file.CreatedTorrent)
                        throw new ArgumentException($"The torrent file is not created for '{fileName}' to copy");
                    if (file.CopiedTorrentFile)
                        throw new ArgumentException($"Torrent file already copied for '{fileName}'");
                    new TorrentUpload().Log($"Copying torrent for queued file. Filename: [{fileName}]",
                        EventLogEntryType.Information);

                    var targetPath = ConfigurationManager.AppSettings["UploadFolder"];
                    var torrentFileName = Path.GetFileName(file.CreatedTorrentFilePath);
                    new TorrentCreate().CopyTorrent(file, targetPath, file.CreatedTorrentFilePath, torrentFileName);

                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET CopiedTorrentFile = True, ProcessedDate = NOW() WHERE ID = {file.Id}");
                    new TorrentUpload().Log(
                        $"Copied torrent file successfully. Filename: [{fileName}]",
                        EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    new TorrentUpload().Log(
                        $"Copying torrent file process not successful, check accdb for error message. Filename: [{file.FilePath}]",
                        EventLogEntryType.Information);
                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET ErrorMessage = '{errorMessage.Replace("'", "''")}' WHERE ID = {file.Id}");
                    throw;
                }

            new TorrentUpload().Log($"Completed copying torrent file for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            return fileName;
        }

        public string DeleteTorrentQueuedFile(int fileId)
        {
            new TorrentUpload().Log($"Starting to delete torrent file for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            var fileList = new TorrentUpload().GetDb(
                $"SELECT * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND ID = {fileId}");

            if (fileList.Count == 0)
                throw new ArgumentException(
                    $"File Id {fileId} not available to complete action, 'Processed' must be set to 'True'.");

            var fileName = string.Empty;
            foreach (var file in fileList)
                try
                {
                    var sourceFile = file.FilePath;
                    fileName = Path.GetFileName(sourceFile);
                    if (!file.CreatedTorrent)
                        throw new ArgumentException($"The torrent file is not created for '{fileName}' to delete");
                    if (file.DeletedTorrentFileAfterEveryThing)
                        throw new ArgumentException($"Torrent file already deleted for '{fileName}'");
                    new TorrentUpload().Log($"Deleting torrent for queued file. Filename: [{fileName}]",
                        EventLogEntryType.Information);

                    var torrentFileName = Path.GetFileName(file.CreatedTorrentFilePath);
                    new TorrentCreate().DeleteTorrent(file, torrentFileName, file.CreatedTorrentFilePath);

                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET DeletedTorrentFileAfterEveryThing = True, ProcessedDate = NOW() WHERE ID = {file.Id}");
                    new TorrentUpload().Log(
                        $"Copied torrent file successfully. Filename: [{fileName}]",
                        EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    new TorrentUpload().Log(
                        $"Deleting torrent file process not successful, check accdb for error message. Filename: [{file.FilePath}]",
                        EventLogEntryType.Information);
                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET ErrorMessage = '{errorMessage.Replace("'", "''")}' WHERE ID = {file.Id}");
                    throw;
                }

            new TorrentUpload().Log($"Completed deleting torrent file for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            return fileName;
        }

        public string MarkQueuedFileAsProcessed(int fileId)
        {
            new TorrentUpload().Log($"Starting process to mark queued file as processed, File Id: {fileId}.",
                EventLogEntryType.Information);

            var fileList = new TorrentUpload().GetDb(
                $"SELECT * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND ID = {fileId}");

            if (fileList.Count == 0)
                throw new ArgumentException(
                    $"File Id {fileId} not available to complete action, 'Processed' must be set to 'True'.");

            var fileName = string.Empty;
            foreach (var file in fileList)
                try
                {
                    var sourceFile = file.FilePath;
                    fileName = Path.GetFileName(sourceFile);
                    if (file.Processed)
                        throw new ArgumentException($"File '{fileName}' already marked as processed");

                    new TorrentUpload().Log($"Marking queued file as processed. Filename: [{fileName}]",
                        EventLogEntryType.Information);

                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET Processed = True, ProcessedDate = NOW() WHERE ID = {file.Id}");
                    new TorrentUpload().Log(
                        $"File marked as processed successfully. Filename: [{fileName}]",
                        EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    var errorMessage = ex.Message;
                    new TorrentUpload().Log(
                        $"Marking file as processed not successful, check accdb for error message. Filename: [{file.FilePath}]",
                        EventLogEntryType.Information);
                    new TorrentUpload().InsertOrUpdateDb(
                        $"UPDATE tTorrentCreatorUploaderFiles SET ErrorMessage = '{errorMessage.Replace("'", "''")}' WHERE ID = {file.Id}");
                    throw;
                }

            new TorrentUpload().Log($"Marked file as processed for queued file, File Id: {fileId}.",
                EventLogEntryType.Information);

            return fileName;
        }

        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
                if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T) field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T) field.GetValue(null);
                }

            throw new ArgumentException($"Description '{description}' not found for enum '{type.Name}'.");
            // or return default(T);
        }

        public static IList EnumUploadTorrentCategoryToList(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var list = new ArrayList();
            var enumValues = Enum.GetValues(type);

            foreach (Enum value in enumValues)
                list.Add(new KeyValuePair<Enum, string>(value, GetEnumDescription((UploadTorrentCategory) value)));

            return list;
        }

        public static IList EnumUploadTorrentLanguageToList(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var list = new ArrayList();
            var enumValues = Enum.GetValues(type);

            foreach (Enum value in enumValues)
                list.Add(new KeyValuePair<Enum, string>(value, GetEnumDescription((UploadTorrentLanguage) value)));

            return list;
        }

        public static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());

            var attributes =
                (DescriptionAttribute[]) fi.GetCustomAttributes(
                    typeof(DescriptionAttribute),
                    false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }

    public class TorrentCreatorUploaderFile
    {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public Guid Guid { get; set; }
        public bool CreatedTorrent { get; set; }
        public bool UploadedTorrent { get; set; }
        public string CreatedTorrentFilePath { get; set; }
        public bool SentTorrentToUTorrentViaWebApi { get; set; }
        public bool SentTorrentToDelugeViaDelugeConsole { get; set; }
        public bool CopiedTorrentFile { get; set; }
        public bool DeletedTorrentFileAfterEveryThing { get; set; }
        public bool Processed { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public int Attempts { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TorrentCreatorUploaderLog
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Type { get; set; }
    }
}