using System;
using System.Collections.Generic;
using System.Configuration;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MonoTorrent.Common;
using TorrentCreatorUploaderLogic;
using TorrentFileSource = TorrentCreatorUploaderLogic.TorrentFileSource;

namespace TorrentCreatorUploader
{
    public partial class MainForm : Form
    {
        private readonly List<FSystemWatcher> _fsWatchers = new List<FSystemWatcher>();
        private bool _creationAborted;
        private IAsyncResult _creationResult;
        private TorrentCreator _torrCreator;

        public MainForm()
        {
            InitializeComponent();

            var assem = Assembly.GetExecutingAssembly();

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                //ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
                //Text = "Torrent Creator Uploader v" + ad.CurrentVersion;
            }

            //else
            //{
            //Text = "Torrent Creator Uploader v" + Application.ProductVersion;
            Text = @"Torrent Creator Uploader v" + assem.GetName().Version;
            //}

            cbUploadTorrent.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["UploadTorrent"]);
            cbPieceSize.SelectedIndex = 0;

            tbAnnounceList.Text = ConfigurationManager.AppSettings["AnnounceList"].Replace(";", Environment.NewLine);
            tbComment.Text = ConfigurationManager.AppSettings["Comment"];

            cbUploadTorrent.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["UploadTorrent"]);
            tbTorrentSiteUrl.Text = ConfigurationManager.AppSettings["TorrentSiteUrl"];
            tbTorrentSiteAccountLoginUrl.Text = ConfigurationManager.AppSettings["TorrentSiteAccountLoginUrl"];
            tbTorrentSiteUploadPageUrl.Text = ConfigurationManager.AppSettings["TorrentSiteUploadPageUrl"];
            tbTorrentSiteUsername.Text = ConfigurationManager.AppSettings["TorrentSiteUsername"];
            tbTorrentSitePassword.Text = ConfigurationManager.AppSettings["TorrentSitePassword"];
            tbUploadTorrentDescription.Text = ConfigurationManager.AppSettings["UploadTorrentDescription"];
            cbUploadTorrentCategoryId.DataSource =
                TorrentUpload.EnumUploadTorrentCategoryToList(typeof(TorrentUpload.UploadTorrentCategory));
            cbUploadTorrentCategoryId.DisplayMember = "Value";
            cbUploadTorrentCategoryId.ValueMember = "Key";
            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["UploadTorrentCategoryId"]))
                cbUploadTorrentCategoryId.SelectedValue = (TorrentUpload.UploadTorrentCategory) Enum.Parse(
                    typeof(TorrentUpload.UploadTorrentCategory),
                    ConfigurationManager.AppSettings["UploadTorrentCategoryId"]);
            cbUploadTorrentLanguageId.DataSource =
                TorrentUpload.EnumUploadTorrentLanguageToList(typeof(TorrentUpload.UploadTorrentLanguage));
            cbUploadTorrentLanguageId.DisplayMember = "Value";
            cbUploadTorrentLanguageId.ValueMember = "Key";
            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["UploadTorrentLanguageId"]))
                cbUploadTorrentLanguageId.SelectedValue = (TorrentUpload.UploadTorrentLanguage) Enum.Parse(
                    typeof(TorrentUpload.UploadTorrentLanguage),
                    ConfigurationManager.AppSettings["UploadTorrentLanguageId"]);
            tbCreatedBy.Text = ConfigurationManager.AppSettings["CreatedBy"];
            tbPublisher.Text = ConfigurationManager.AppSettings["Publisher"];
            tbPublisherUrl.Text = ConfigurationManager.AppSettings["PublisherUrl"];
            cbPieceSize.Text = ConfigurationManager.AppSettings["PieceSize"];
            cbPrivate.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["Private"]);
            cbStoreMD5.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["StoreMD5"]);
            cbIgnoreHidden.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["IgnoreHidden"]);

            cbSendTorrentToUTorrentViaWebApi.Checked =
                Convert.ToBoolean(ConfigurationManager.AppSettings["SendTorrentToUTorrentViaWebApi"]);
            tbuTorrentIP.Text = ConfigurationManager.AppSettings["uTorrentIP"];
            tbuTorrentPort.Text = ConfigurationManager.AppSettings["uTorrentPort"];
            tbuTorrentUsername.Text = ConfigurationManager.AppSettings["uTorrentUsername"];
            tbuTorrentPassword.Text = ConfigurationManager.AppSettings["uTorrentPassword"];

            cbSendTorrentToDelugeViaDelugeConsole.Checked =
                Convert.ToBoolean(ConfigurationManager.AppSettings["SendTorrentToDelugeViaDelugeConsole"]);
            tbDelugeIP.Text = ConfigurationManager.AppSettings["DelugeIP"];
            tbDelugePort.Text = ConfigurationManager.AppSettings["DelugePort"];
            tbDelugeUsername.Text = ConfigurationManager.AppSettings["DelugeUsername"];
            tbDelugePassword.Text = ConfigurationManager.AppSettings["DelugePassword"];
            tbDelugeInstalledFolder.Text = ConfigurationManager.AppSettings["DelugeInstalledFolder"];

            cbCopyTorrentFile.Checked = Convert.ToBoolean(ConfigurationManager.AppSettings["CopyTorrentFile"]);
            tbCopyTorrentFile.Text = ConfigurationManager.AppSettings["CopyTorrentFilePath"];

            cbDeleteTorrentFileAfterEverything.Checked =
                Convert.ToBoolean(ConfigurationManager.AppSettings["DeleteTorrentFileAfterEveryThing"]);
        }

        public sealed override string Text
        {
            get => base.Text;
            set => base.Text = value;
        }

        private void bCreate_Click(object sender, EventArgs e)
        {
            //Error Check the required values
            if (string.IsNullOrWhiteSpace(tbSource.Text) ||
                !Directory.Exists(tbSource.Text) && !File.Exists(tbSource.Text))
            {
                MessageBox.Show(this, @"Source not valid.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Create our torrent
            _torrCreator = new TorrentCreator();

            if (!string.IsNullOrWhiteSpace(tbAnnounceList.Text))
                foreach (var a in tbAnnounceList.Lines)
                    if (!string.IsNullOrWhiteSpace(a))
                        _torrCreator.Announces.Add(a.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries)
                            .ToList());
            //torrCreator.Announces.Add(new MonoTorrent.RawTrackerTier(A.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries)));
            if (!string.IsNullOrWhiteSpace(tbComment.Text))
                _torrCreator.Comment = tbComment.Text;
            if (!string.IsNullOrWhiteSpace(tbCreatedBy.Text))
                _torrCreator.CreatedBy = tbCreatedBy.Text;
            if (!string.IsNullOrWhiteSpace(tbPublisher.Text))
                _torrCreator.Publisher = tbPublisher.Text;
            if (!string.IsNullOrWhiteSpace(tbPublisherUrl.Text))
                _torrCreator.PublisherUrl = tbPublisherUrl.Text;
            if (cbPieceSize.SelectedIndex != 0)
                _torrCreator.PieceLength = (long) Math.Pow(2, cbPieceSize.SelectedIndex + 13);
            if (!string.IsNullOrWhiteSpace(tbCustom.Text))
                if ((from a in tbCustom.Lines
                        where !string.IsNullOrWhiteSpace(a)
                        select a.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries))
                    .Any(customValue => customValue.Length != 2))
                {
                    MessageBox.Show(this, @"Custom value not valid.", @"Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

            if (!string.IsNullOrWhiteSpace(tbCustomSecure.Text))
                if ((from a in tbCustomSecure.Lines
                        where !string.IsNullOrWhiteSpace(a)
                        select a.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries))
                    .Any(customValue => customValue.Length != 2))
                {
                    MessageBox.Show(this, @"Custom value not valid.", @"Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }


            _torrCreator.Private = cbPrivate.Checked;
            _torrCreator.StoreMD5 = cbStoreMD5.Checked;

            //Create the update progress function
            _torrCreator.Hashed += delegate(object o, TorrentCreatorEventArgs e2)
            {
                if (!double.IsNaN(e2.OverallCompletion))
                    pbOverall.Invoke((Action) (() => pbOverall.Value = e2.OverallCompletion >= pbOverall.Maximum
                        ? pbOverall.Maximum
                        : (e2.OverallCompletion <= pbOverall.Minimum
                            ? pbOverall.Minimum
                            : (int) e2.OverallCompletion)));
            };


            //Reset the progress state for a new run, and start saving the torrent asynchronously
            bClose.Text = @"Cancel";
            gbSource.Enabled = gbProperties.Enabled = gbAdvProps.Enabled = gbOther.Enabled = gbTorrentSite.Enabled =
                gbCopyTorrentFile.Enabled =
                    gbDeleteTorrentFileAfterEverything.Enabled =
                        gbuTorrent.Enabled = gbDeluge.Enabled = gbFileWatcher.Enabled = false;
            bCreate.Enabled = false;
            _creationResult =
                _torrCreator.BeginCreate(new TorrentFileSource(tbSource.Text, cbIgnoreHidden.Checked, tbSkipFiles.Text),
                    TorrentCreated, _torrCreator);
        }

        private void TorrentCreated(IAsyncResult result)
        {
            //Invoke the main thread and reset the gui\save the torrent
            Invoke((Action) (() =>
            {
                var sourceFile = tbSource.Text;

                //Save the result if needed
                try
                {
                    //Save the torrent file
                    if (result.IsCompleted && !_creationAborted)
                        using (var saveFile = new SaveFileDialog
                        {
                            FileName = Path.GetFileName(tbSource.Text) + ".torrent",
                            InitialDirectory = Path.GetDirectoryName(tbSource.Text + ".fileparentdirectory"),
                            CheckFileExists = false,
                            OverwritePrompt = true,
                            DefaultExt = "torrent",
                            AddExtension = true,
                            Filter = @"torrent files (*.torrent)|*.torrent|All files (*.*)|*.*",
                            FilterIndex = 0
                        })
                        {
                            if (saveFile.ShowDialog(this) == DialogResult.OK)
                            {
                                var guid = Guid.NewGuid();
                                var torrentFilePath =
                                    saveFile.FileName.Replace(Path.GetFileName(saveFile.FileName),
                                        Convert.ToString(guid)) + ".torrent";
                                (result.AsyncState as TorrentCreator)?.EndCreate(result, torrentFilePath);
                                var destFile = saveFile.FileName.Replace(".torrent", "");
                                MessageBox.Show(this, @"Torrent file saved successfully.", @"Info",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);

                                //Upload section

                                var uploadTorrent = cbUploadTorrent.Checked;
                                var sendTorrentToUTorrentViaWebApi = cbSendTorrentToUTorrentViaWebApi.Checked;
                                var sendTorrentToDelugeViaDelugeConsole = cbSendTorrentToDelugeViaDelugeConsole.Checked;
                                var copyTorrentFile = cbCopyTorrentFile.Checked;
                                var deleteTorrentFileAfterEverything = cbDeleteTorrentFileAfterEverything.Checked;

                                if (uploadTorrent)
                                {
                                    var torrentSiteUrl = tbTorrentSiteUrl.Text;
                                    var torrentSiteUsername = tbTorrentSiteUsername.Text;
                                    var torrentSitePassword = tbTorrentSitePassword.Text;
                                    var torrentSiteAccountLoginUrl = tbTorrentSiteAccountLoginUrl.Text;
                                    var torrentSiteUploadPageUrl = tbTorrentSiteUploadPageUrl.Text;
                                    var uploadTorrentDescription = tbUploadTorrentDescription.Text;
                                    var category =
                                        (TorrentUpload.UploadTorrentCategory) ((KeyValuePair<Enum, string>)
                                            cbUploadTorrentCategoryId.SelectedItem).Key;
                                    var uploadTorrentCategoryId = Convert.ToString(Convert.ToInt32(category));
                                    var language =
                                        (TorrentUpload.UploadTorrentLanguage) ((KeyValuePair<Enum, string>)
                                            cbUploadTorrentLanguageId.SelectedItem).Key;
                                    var uploadTorrentLanguageId = Convert.ToString(Convert.ToInt32(language));

                                    new TorrentUpload().Log(
                                        $"Starting process of uploading torrent to torrent site via RestSharp. Filename: [{Path.GetFileName($"{destFile}.torrent")}]",
                                        EventLogEntryType.Information);
                                    new TorrentUpload().UploadTorrentViaRestSharp(destFile, sourceFile, torrentSiteUrl,
                                        torrentSiteUsername, torrentSitePassword, torrentSiteAccountLoginUrl,
                                        torrentSiteUploadPageUrl, uploadTorrentDescription, uploadTorrentCategoryId,
                                        uploadTorrentLanguageId, tbUploadTorrentNfoFile.Text,
                                        tbUploadTorrentImageFile.Text, guid);
                                    MessageBox.Show(this, @"Torrent file uploaded successfully.", @"Info",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }

                                if (sendTorrentToUTorrentViaWebApi)
                                {
                                    var uTorrentIp = tbuTorrentIP.Text;
                                    var uTorrentPort = Convert.ToInt32(tbuTorrentPort.Text);
                                    var uTorrentUsername = tbuTorrentUsername.Text;
                                    var uTorrentPassword = tbuTorrentPassword.Text;

                                    new TorrentUpload().Log(
                                        $"Starting process of sending torrent via uTorrent Web UI. Filename: [{Path.GetFileName($"{destFile}.torrent")}]",
                                        EventLogEntryType.Information);
                                    new TorrentUpload().SendTorrentToUTorrentViaWebUi(destFile, uTorrentIp,
                                        uTorrentPort, uTorrentUsername, uTorrentPassword,
                                        guid);
                                    MessageBox.Show(this, @"Torrent file sent to uTorrent successfully.", @"Info",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }

                                if (sendTorrentToDelugeViaDelugeConsole)
                                {
                                    var delugeIp = tbDelugeIP.Text;
                                    var delugePort = Convert.ToInt32(tbDelugePort.Text);
                                    var delugeUsername = tbDelugeUsername.Text;
                                    var delugePassword = tbDelugePassword.Text;
                                    var delugeInstalledFolder = tbDelugeInstalledFolder.Text;

                                    new TorrentUpload().Log(
                                        $"Starting process of sending torrent via Deluge debug console. Filename: [{Path.GetFileName($"{destFile}.torrent")}]",
                                        EventLogEntryType.Information);
                                    new TorrentUpload().SendTorrentToDelugeViaDelugeConsole(destFile, delugeIp,
                                        delugePort, delugeUsername, delugePassword, delugeInstalledFolder, guid);
                                    MessageBox.Show(this, @"Torrent file sent to Deluge successfully.", @"Info",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }

                                if (copyTorrentFile)
                                {
                                    new TorrentUpload().Log(
                                        $"Starting process of copying .torrent file. Filename: [{Path.GetFileName(torrentFilePath)}]",
                                        EventLogEntryType.Information);
                                    if (File.Exists(torrentFilePath))
                                    {
                                        new TorrentUpload().Log(
                                            $"Copying .torrent file. Filename: [{Path.GetFileName(torrentFilePath)}]",
                                            EventLogEntryType.Information);
                                        var copyTorrentFilePath = tbCopyTorrentFile.Text;
                                        if (!Directory.Exists(copyTorrentFilePath))
                                            Directory.CreateDirectory(copyTorrentFilePath);
                                        var destTorrentFile = Path.Combine(copyTorrentFilePath,
                                            Path.GetFileName(torrentFilePath));
                                        File.Copy(torrentFilePath, destTorrentFile, true);
                                        new TorrentUpload().Log(
                                            $"File copied to upload folder. [Path: {copyTorrentFilePath}]",
                                            EventLogEntryType.Information);
                                        MessageBox.Show(this, @"Torrent file copied successfully.", @"Info",
                                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    else
                                    {
                                        new TorrentUpload().Log(
                                            $"Torrent file does not exit to copy. Path: [{torrentFilePath}.torrent]",
                                            EventLogEntryType.Information);
                                    }
                                }

                                if (deleteTorrentFileAfterEverything)
                                {
                                    new TorrentUpload().Log(
                                        $"Starting process of deleting .torrent file. Filename: [{Path.GetFileName(torrentFilePath)}]",
                                        EventLogEntryType.Information);
                                    if (File.Exists(torrentFilePath))
                                    {
                                        new TorrentUpload().Log(
                                            $"Deleting .torrent file. Filename: [{Path.GetFileName(torrentFilePath)}]",
                                            EventLogEntryType.Information);
                                        File.Delete(torrentFilePath);
                                        MessageBox.Show(this, @"Torrent file deleted successfully.", @"Info",
                                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                            }
                        }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, @"Error creating torrent: " + ex + @".", @"Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                //Have the main forum reset the gui
                pbOverall.Value = pbOverall.Minimum;
                _creationAborted = false;

                bClose.Text = @"Close";
                gbSource.Enabled = gbProperties.Enabled = gbAdvProps.Enabled = gbOther.Enabled = gbTorrentSite.Enabled =
                    gbCopyTorrentFile.Enabled =
                        gbDeleteTorrentFileAfterEverything.Enabled =
                            gbuTorrent.Enabled = gbDeluge.Enabled = gbFileWatcher.Enabled = true;
                bCreate.Enabled = true;
                _creationResult = null;
            }));
        }

        private void bClose_Click(object sender, EventArgs e)
        {
            if (_creationResult != null)
            {
                if (_creationResult.IsCompleted || _creationAborted) return;
                _creationAborted = true;
                _torrCreator.AbortCreation();
            }
            else
            {
                Application.Exit();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_creationResult != null)
                bClose_Click(sender, null);
        }

        private void bFolder_Click(object sender, EventArgs e)
        {
            using (var sourceFolder = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                RootFolder = Environment.SpecialFolder.MyComputer,
                Description = @"Choose a source folder."
            })
            {
                if (sourceFolder.ShowDialog(this) == DialogResult.OK)
                    tbSource.Text = sourceFolder.SelectedPath;
            }
        }

        private void bFile_Click(object sender, EventArgs e)
        {
            using (var sourceFile = new OpenFileDialog {CheckFileExists = true, Multiselect = false})
            {
                if (sourceFile.ShowDialog(this) == DialogResult.OK)
                    tbSource.Text = sourceFile.FileName;
            }
        }

        private void bDelugeInstalledFolder_Click(object sender, EventArgs e)
        {
            using (var sourceFolder = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                RootFolder = Environment.SpecialFolder.MyComputer,
                SelectedPath = tbDelugeInstalledFolder.Text,
                Description = @"Choose the Deluge installed folder."
            })
            {
                if (sourceFolder.ShowDialog(this) == DialogResult.OK)
                    tbDelugeInstalledFolder.Text = sourceFolder.SelectedPath;
            }
        }

        private void bCopyTorrentFile_Click(object sender, EventArgs e)
        {
            using (var sourceFolder = new FolderBrowserDialog
            {
                ShowNewFolderButton = false,
                RootFolder = Environment.SpecialFolder.MyComputer,
                Description = @"Choose the folder to copy the torrent file to."
            })
            {
                if (sourceFolder.ShowDialog(this) == DialogResult.OK)
                    tbCopyTorrentFile.Text = sourceFolder.SelectedPath;
            }
        }

        private void bUploadTorrentNfoFile_Click(object sender, EventArgs e)
        {
            using (var sourceFile = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = @"nfo files (*.nfo)|*.nfo"
            })
            {
                if (sourceFile.ShowDialog(this) == DialogResult.OK)
                    tbUploadTorrentNfoFile.Text = sourceFile.FileName;
            }
        }

        private void bUploadTorrentImageFile_Click(object sender, EventArgs e)
        {
            using (var sourceFile = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Filter = @"jpg files (*.jpg)|*.jpg|jpeg files (*.jpeg*)|*.jpeg*"
            })
            {
                if (sourceFile.ShowDialog(this) == DialogResult.OK)
                    tbUploadTorrentImageFile.Text = sourceFile.FileName;
            }
        }

        private void tbUploadTorrentNfoFile_MouseHover(object sender, EventArgs e)
        {
            var toolTipUploadTorrentNfoFile = new ToolTip();
            toolTipUploadTorrentNfoFile.SetToolTip(tbUploadTorrentNfoFile, tbUploadTorrentNfoFile.Text);
        }

        private void tbUploadTorrentImageFile_MouseHover(object sender, EventArgs e)
        {
            var toolTipUploadTorrentImageFile = new ToolTip();
            toolTipUploadTorrentImageFile.SetToolTip(tbUploadTorrentImageFile, tbUploadTorrentImageFile.Text);
        }

        private void bFileWatcherEnable_Click(object sender, EventArgs e)
        {
            var paths = ConfigurationManager.AppSettings["DownloadFolder"]
                .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var path in paths)
            {
                var fsWatcher = new FSystemWatcher(path);
                _fsWatchers.Add(fsWatcher);
            }

            foreach (var fsWatcher in _fsWatchers) fsWatcher.StartFsWatcherService();
            MessageBox.Show(this, @"File Watcher Enabled.", @"Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            bFileWatcherEnable.Enabled = false;
            bFileWatcherDisable.Enabled = true;
        }

        private void bFileWatcherDisable_Click(object sender, EventArgs e)
        {
            foreach (var fsWatcher in _fsWatchers) fsWatcher.StopFsWatcherService();
            MessageBox.Show(this, @"File Watcher Disabled.", @"Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            bFileWatcherEnable.Enabled = true;
            bFileWatcherDisable.Enabled = false;
        }
    }
}