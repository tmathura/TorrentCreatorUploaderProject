using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;

namespace TorrentCreatorUploaderLogic
{
    public class TelegramUtil
    {
        private TelegramBotClient _telegramBot;

        public void Instantiate()
        {
            var accessToken = ConfigurationManager.AppSettings["TelegramAccessToken"];

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                new TorrentUpload().Log("Telegram: Failed to instantiate Telegram, Telegram Access Token is blank.",
                    EventLogEntryType.Warning);
            }
            else
            {
                _telegramBot = new TelegramBotClient(accessToken);
                var meBot = _telegramBot.GetMeAsync().Result;
                _telegramBot.OnCallbackQuery += BotOnCallbackQueryReceived;
                _telegramBot.OnMessage += BotOnMessageReceived;
                _telegramBot.OnMessageEdited += BotOnMessageReceived;
                _telegramBot.StartReceiving();
            }
        }

        public async Task SendTelegramMsg(string accessToken, string msg)
        {
            try
            {
                var botClient = new TelegramBotClient(accessToken);
                var chatId = Convert.ToInt32(ConfigurationManager.AppSettings["TelegramUserChatId"]);
                if (chatId == 0)
                    new TorrentUpload().Log(
                        "Telegram: Failed to send message to Telegram, Chat Id is 0, did you send a /start message after starting the service?",
                        EventLogEntryType.Warning);
                else
                    await botClient.SendTextMessageAsync(chatId,
                        msg.Substring(0, Math.Min(msg.Replace("_", "").Length, 4096)));
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log($"Telegram: Failed to send message to Telegram - {ex.Message}",
                    EventLogEntryType.Warning);
            }
        }

        public async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            try
            {
                var callbackQuery = callbackQueryEventArgs.CallbackQuery;
                try
                {
                    string fileName;
                    var callBackCommand = callbackQuery.Data.Split('#')[0];
                    var callBackData = callbackQuery.Data.Split('#')[1];

                    switch (callBackCommand)
                    {
                        case "ProcessFile":
                            fileName = new TorrentUpload().ProcessQueuedFile(Convert.ToInt32(callBackData));
                            await _telegramBot.AnswerCallbackQueryAsync(
                                callbackQuery.Id,
                                $"Full file process for file '{fileName}'.");
                            new TorrentUpload().Log($"Full file process for file '{fileName}'.",
                                EventLogEntryType.Information);
                            break;
                        case "CreateTorrent":
                            fileName = new TorrentUpload().CreateTorrentQueuedFile(Convert.ToInt32(callBackData));
                            await _telegramBot.AnswerCallbackQueryAsync(
                                callbackQuery.Id,
                                $"Created torrent file for '{fileName}'.");
                            new TorrentUpload().Log($"Created torrent file for '{fileName}'.",
                                EventLogEntryType.Information);
                            break;
                        case "UploadTorrent":
                            fileName = new TorrentUpload().UploadTorrentQueuedFile(Convert.ToInt32(callBackData));
                            await _telegramBot.AnswerCallbackQueryAsync(
                                callbackQuery.Id,
                                $"Uploaded torrent file '{fileName}'.torrent.");
                            new TorrentUpload().Log($"Uploaded torrent file '{fileName}'.torrent.",
                                EventLogEntryType.Information);
                            break;
                        case "SendToUTorrent":
                            fileName = new TorrentUpload().UTorrentUploadTorrentQueuedFile(
                                Convert.ToInt32(callBackData));
                            await _telegramBot.AnswerCallbackQueryAsync(
                                callbackQuery.Id,
                                $"Sent torrent for '{fileName}'.torrent to uTorrent.");
                            new TorrentUpload().Log($"Sent torrent for '{fileName}'.torrent to uTorrent.",
                                EventLogEntryType.Information);
                            break;
                        case "SendToDeluge":
                            fileName = new TorrentUpload().DelugeUploadTorrentQueuedFile(Convert.ToInt32(callBackData));
                            await _telegramBot.AnswerCallbackQueryAsync(
                                callbackQuery.Id,
                                $"Sent torrent for '{fileName}'.torrent to Deluge.");
                            new TorrentUpload().Log($"Sent torrent for '{fileName}'.torrent to Deluge.",
                                EventLogEntryType.Information);
                            break;
                        case "CopyTorrent":
                            fileName = new TorrentUpload().CopyTorrentQueuedFile(Convert.ToInt32(callBackData));
                            await _telegramBot.AnswerCallbackQueryAsync(
                                callbackQuery.Id,
                                $"Torrent '{fileName}'.torrent copied.");
                            new TorrentUpload().Log($"Torrent '{fileName}'.torrent copied.",
                                EventLogEntryType.Information);
                            break;
                        case "DeleteTorrent":
                            fileName = new TorrentUpload().DeleteTorrentQueuedFile(Convert.ToInt32(callBackData));
                            await _telegramBot.AnswerCallbackQueryAsync(
                                callbackQuery.Id,
                                $"Torrent for '{fileName}'.torrent deleted.");
                            new TorrentUpload().Log($"Torrent for '{fileName}'.torrent deleted.",
                                EventLogEntryType.Information);
                            break;
                        case "MarkFileAsProcessed":
                            fileName = new TorrentUpload().MarkQueuedFileAsProcessed(Convert.ToInt32(callBackData));
                            await _telegramBot.AnswerCallbackQueryAsync(
                                callbackQuery.Id,
                                $"File '{fileName}' marked as processed.");
                            new TorrentUpload().Log($"File '{fileName}' marked as processed.",
                                EventLogEntryType.Information);
                            break;
                        case "InvalidActionResponse":
                            await _telegramBot.AnswerCallbackQueryAsync(
                                callbackQuery.Id,
                                callBackData);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    new TorrentUpload().Log("Error: " + ex.Message + ".", EventLogEntryType.Error);
                    await _telegramBot.AnswerCallbackQueryAsync(
                        callbackQuery.Id,
                        $"Processing file error: {ex.Message.Substring(0, Math.Min(ex.Message.Replace("_", "").Length, 100))}");
                }
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log("Error: Failed with AnswerCallbackQueryAsync - " + ex.Message + ".",
                    EventLogEntryType.Error);
            }
        }

        public async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                string msg;
                var message = messageEventArgs.Message;
                var text = message.Text;
                var chatId = messageEventArgs.Message.Chat.Id;
                var fileSystemWatchAndProcessValidFileProcessAttempts = Convert.ToInt32(
                    ConfigurationManager.AppSettings["FileSystemWatchAndProcessValidFileProcessAttempts"]);

                if (message == null || message.Type != MessageType.TextMessage) return;

                if (text == "/start")
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

                    xmlDoc.SelectSingleNode("//appSettings/add[@key='TelegramUserChatId']").Attributes["value"].Value =
                        Convert.ToString(chatId);
                    xmlDoc.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

                    ConfigurationManager.RefreshSection("appSettings");

                    msg = "Welcome...Here are all my commands";
                    await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Default,
                        replyMarkup: CustomerKeyBoard());
                    return;
                }

                if (text == "/help")
                {
                    msg = "Here are all my commands";
                    await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Default,
                        replyMarkup: CustomerKeyBoard());
                    return;
                }

                if (text.Contains("/get_unprocessed"))
                {
                    var arguments = text.Split(null);
                    if (arguments.Length > 3)
                    {
                        msg = "Too many arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    if (arguments.Length < 3)
                    {
                        msg = "Too few arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var order = arguments[1].ToUpper();
                    var words = new List<string> {"[ASC]", "[DESC]"};
                    var pattern = string.Join("|", words.Select(Regex.Escape));
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var isValidOrderSeq = regex.IsMatch(order);
                    if (!isValidOrderSeq)
                    {
                        msg = "Order argument not valid, it must be either ASC or DESC.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    order = order.Replace("[", "").Replace("]", "");

                    var number = arguments[2].Replace("[", "").Replace("]", "");
                    var isInt = int.TryParse(number, out _);
                    if (!isInt)
                    {
                        msg = "Number argument not valid, it must be a number.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var telegramMsg = new List<string>();
                    var fileList = new TorrentUpload().GetDb(
                        $"SELECT TOP {number} * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND Attempts < {fileSystemWatchAndProcessValidFileProcessAttempts} ORDER BY CreatedDate {order}");
                    foreach (var file in fileList)
                        telegramMsg.Add(
                            $"Id: {file.Id}" +
                            $"\r\nFile: {Path.GetFileName(file.FilePath)?.Replace("_", "")}" +
                            $"\r\nCreated Torrent: {GetEmoticonBool(file.CreatedTorrent)}" +
                            $"\r\nUploaded Torrent: {GetEmoticonBool(file.UploadedTorrent)}" +
                            $"\r\nSent Torrent To uTorrent Via Web Api: {GetEmoticonBool(file.SentTorrentToUTorrentViaWebApi)}" +
                            $"\r\nSent Torrent To Deluge Via Deluge Console: {GetEmoticonBool(file.SentTorrentToDelugeViaDelugeConsole)}" +
                            $"\r\nCopied Torrent File: {GetEmoticonBool(file.CopiedTorrentFile)}" +
                            $"\r\nDeleted Torrent File After EveryThing: {GetEmoticonBool(file.DeletedTorrentFileAfterEveryThing)}" +
                            $"\r\nProcessed: {GetEmoticonBool(file.Processed)}" +
                            $"\r\nProcessed Date: {file.ProcessedDate}" +
                            $"\r\nAttempts: {GetEmoticonInt(file.Attempts)}" +
                            $"\r\nErrorMessage: {file.ErrorMessage?.Substring(0, Math.Min(file.ErrorMessage.Replace("_", "").Length, 4096))}" +
                            $"\r\nCreated Date: {file.CreatedDate}" +
                            string.Format("\r\n{0}{0}{0}{0}{0}{0}{0}{0}{0}{0}", "\u2796")
                        );

                    if (telegramMsg.Count == 0) telegramMsg.Add("No unprocessed items.");

                    var result = string.Join("\r\n", telegramMsg.ToArray());
                    msg = result;
                    await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                        replyMarkup: CustomerKeyBoard());
                    return;
                }

                if (text.Contains("/get_processed"))
                {
                    var arguments = text.Split(null);
                    if (arguments.Length > 3)
                    {
                        msg = "Too many arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    if (arguments.Length < 3)
                    {
                        msg = "Too few arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var order = arguments[1].ToUpper();
                    var words = new List<string> {"[ASC]", "[DESC]"};
                    var pattern = string.Join("|", words.Select(Regex.Escape));
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var isValidOrderSeq = regex.IsMatch(order);
                    if (!isValidOrderSeq)
                    {
                        msg = "Order argument not valid, it must be either ASC or DESC.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    order = order.Replace("[", "").Replace("]", "");

                    var number = arguments[2].Replace("[", "").Replace("]", "");
                    var isInt = int.TryParse(number, out _);
                    if (!isInt)
                    {
                        msg = "Number argument not valid, it must be a number.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var telegramMsg = new List<string>();
                    var fileList = new TorrentUpload().GetDb(
                        $"SELECT TOP {number} * FROM tTorrentCreatorUploaderFiles WHERE Processed = True ORDER BY CreatedDate {order}");
                    foreach (var file in fileList)
                        telegramMsg.Add(
                            $"Id: {file.Id}" +
                            $"\r\nFile: {Path.GetFileName(file.FilePath)?.Replace("_", "")}" +
                            $"\r\nCreated Torrent: {GetEmoticonBool(file.CreatedTorrent)}" +
                            $"\r\nUploaded Torrent: {GetEmoticonBool(file.UploadedTorrent)}" +
                            $"\r\nSent Torrent To uTorrent Via Web Api: {GetEmoticonBool(file.SentTorrentToUTorrentViaWebApi)}" +
                            $"\r\nSent Torrent To Deluge Via Deluge Console: {GetEmoticonBool(file.SentTorrentToDelugeViaDelugeConsole)}" +
                            $"\r\nCopied Torrent File: {GetEmoticonBool(file.CopiedTorrentFile)}" +
                            $"\r\nDeleted Torrent File After EveryThing: {GetEmoticonBool(file.DeletedTorrentFileAfterEveryThing)}" +
                            $"\r\nProcessed: {GetEmoticonBool(file.Processed)}" +
                            $"\r\nProcessed Date: {file.ProcessedDate}" +
                            $"\r\nAttempts: {GetEmoticonInt(file.Attempts)}" +
                            $"\r\nErrorMessage: {file.ErrorMessage?.Substring(0, Math.Min(file.ErrorMessage.Replace("_", "").Length, 4096))}" +
                            $"\r\nCreated Date: {file.CreatedDate}" +
                            string.Format("\r\n{0}{0}{0}{0}{0}{0}{0}{0}{0}{0}", "\u2796")
                        );

                    if (telegramMsg.Count == 0) telegramMsg.Add("No processed items.");

                    var result = string.Join("\r\n", telegramMsg.ToArray());
                    msg = result;
                    await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                        replyMarkup: CustomerKeyBoard());
                }

                if (text.Contains("/get_logs_error"))
                {
                    var arguments = text.Split(null);
                    if (arguments.Length > 3)
                    {
                        msg = "Too many arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    if (arguments.Length < 3)
                    {
                        msg = "Too few arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var order = arguments[1].ToUpper();
                    var words = new List<string> {"[ASC]", "[DESC]"};
                    var pattern = string.Join("|", words.Select(Regex.Escape));
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var isValidOrderSeq = regex.IsMatch(order);
                    if (!isValidOrderSeq)
                    {
                        msg = "Order argument not valid, it must be either ASC or DESC.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    order = order.Replace("[", "").Replace("]", "");

                    var number = arguments[2].Replace("[", "").Replace("]", "");
                    var isInt = int.TryParse(number, out _);
                    if (!isInt)
                    {
                        msg = "Number argument not valid, it must be a number.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var telegramMsg = new List<string>();
                    var fileList = new TorrentUpload().GetDbLogs(
                        $"SELECT TOP {number} * FROM tTorrentCreatorUploaderLog WHERE Type = 'Error' ORDER BY CreatedDate {order}");
                    foreach (var file in fileList)
                        telegramMsg.Add(
                            $"\r\nType: \u26D4 {file.Type}" +
                            $"\r\nMessage: {file.Message.Replace("_", "").Substring(0, Math.Min(file.Message.Replace("_", "").Length, 4096))}" +
                            $"\r\nCreated Date: {file.CreatedDate}" +
                            string.Format("\r\n{0}{0}{0}{0}{0}{0}{0}{0}{0}{0}", "\u2796")
                        );

                    if (telegramMsg.Count == 0) telegramMsg.Add("No error logs.");

                    var result = string.Join("\r\n", telegramMsg.ToArray());
                    msg = result;
                    await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                        replyMarkup: CustomerKeyBoard());
                }

                if (text.Contains("/get_logs_info"))
                {
                    var arguments = text.Split(null);
                    if (arguments.Length > 3)
                    {
                        msg = "Too many arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    if (arguments.Length < 3)
                    {
                        msg = "Too few arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var order = arguments[1].ToUpper();
                    var words = new List<string> {"[ASC]", "[DESC]"};
                    var pattern = string.Join("|", words.Select(Regex.Escape));
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var isValidOrderSeq = regex.IsMatch(order);
                    if (!isValidOrderSeq)
                    {
                        msg = "Order argument not valid, it must be either ASC or DESC.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    order = order.Replace("[", "").Replace("]", "");

                    var number = arguments[2].Replace("[", "").Replace("]", "");
                    var isInt = int.TryParse(number, out _);
                    if (!isInt)
                    {
                        msg = "Number argument not valid, it must be a number.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var telegramMsg = new List<string>();
                    var fileList = new TorrentUpload().GetDbLogs(
                        $"SELECT TOP {number} * FROM tTorrentCreatorUploaderLog WHERE Type = 'Information' ORDER BY CreatedDate {order}");
                    foreach (var file in fileList)
                        telegramMsg.Add(
                            $"\r\nType: \u2139 {file.Type}" +
                            $"\r\nMessage: {file.Message.Replace("_", "").Substring(0, Math.Min(file.Message.Replace("_", "").Length, 4096))}" +
                            $"\r\nCreated Date: {file.CreatedDate}" +
                            string.Format("\r\n{0}{0}{0}{0}{0}{0}{0}{0}{0}{0}", "\u2796")
                        );

                    if (telegramMsg.Count == 0) telegramMsg.Add("No information logs.");

                    var result = string.Join("\r\n", telegramMsg.ToArray());
                    msg = result;
                    await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                        replyMarkup: CustomerKeyBoard());
                }

                if (text.Contains("/process_a_queued_file"))
                {
                    var arguments = text.Split(null);
                    if (arguments.Length > 3)
                    {
                        msg = "Too many arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    if (arguments.Length < 3)
                    {
                        msg = "Too few arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var order = arguments[1].ToUpper();
                    var words = new List<string> {"[ASC]", "[DESC]"};
                    var pattern = string.Join("|", words.Select(Regex.Escape));
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var isValidOrderSeq = regex.IsMatch(order);
                    if (!isValidOrderSeq)
                    {
                        msg = "Order argument not valid, it must be either ASC or DESC.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    order = order.Replace("[", "").Replace("]", "");

                    var number = arguments[2].Replace("[", "").Replace("]", "");
                    var isInt = int.TryParse(number, out _);
                    if (!isInt)
                    {
                        msg = "Number argument not valid, it must be a number.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    await _telegramBot.SendChatActionAsync(chatId, ChatAction.Typing);

                    var fileList = new TorrentUpload().GetDb(
                        $"SELECT TOP {number} * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND Attempts < {fileSystemWatchAndProcessValidFileProcessAttempts} ORDER BY CreatedDate {order}");

                    if (fileList.Count == 0)
                        await _telegramBot.SendTextMessageAsync(chatId, "No unprocessed items to process");
                    else
                        await _telegramBot.SendTextMessageAsync(
                            chatId,
                            "Choose a file to process",
                            replyMarkup: CreateInLineQueuedMarkup(fileList));
                }

                if (text.Contains("/process_an_in_error_file"))
                {
                    var arguments = text.Split(null);
                    if (arguments.Length > 3)
                    {
                        msg = "Too many arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    if (arguments.Length < 3)
                    {
                        msg = "Too few arguments passed for this command.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    var order = arguments[1].ToUpper();
                    var words = new List<string> {"[ASC]", "[DESC]"};
                    var pattern = string.Join("|", words.Select(Regex.Escape));
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    var isValidOrderSeq = regex.IsMatch(order);
                    if (!isValidOrderSeq)
                    {
                        msg = "Order argument not valid, it must be either ASC or DESC.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    order = order.Replace("[", "").Replace("]", "");

                    var number = arguments[2].Replace("[", "").Replace("]", "");
                    var isInt = int.TryParse(number, out _);
                    if (!isInt)
                    {
                        msg = "Number argument not valid, it must be a number.";
                        await _telegramBot.SendTextMessageAsync(chatId, msg, ParseMode.Markdown,
                            replyMarkup: CustomerKeyBoard());
                        return;
                    }

                    await _telegramBot.SendChatActionAsync(chatId, ChatAction.Typing);

                    var fileList = new TorrentUpload().GetDb(
                        $"SELECT TOP {number} * FROM tTorrentCreatorUploaderFiles WHERE Processed = False AND Attempts >= {fileSystemWatchAndProcessValidFileProcessAttempts} ORDER BY CreatedDate {order}");

                    if (fileList.Count == 0)
                        await _telegramBot.SendTextMessageAsync(chatId, "No unprocessed items in error to process");
                    else
                        await _telegramBot.SendTextMessageAsync(
                            chatId,
                            "Choose a file to process",
                            replyMarkup: CreateInLineQueuedMarkup(fileList));
                }
            }
            catch (Exception ex)
            {
                new TorrentUpload().Log($"Telegram: Failed with message from Telegram - {ex.Message}",
                    EventLogEntryType.Error);
            }
        }

        public static IReplyMarkup CreateInlineKeyboardButton(Dictionary<string, string> buttonList, int columns)
        {
            var rows = (int) Math.Ceiling(buttonList.Count / (double) columns);
            var buttons = new InlineKeyboardButton[rows][];

            for (var i = 0; i < buttons.Length; i++)
                buttons[i] = buttonList
                    .Skip(i * columns)
                    .Take(columns)
                    .Select(direction => new InlineKeyboardCallbackButton(
                        direction.Value, direction.Key
                    ))
                    .ToArray();
            return new InlineKeyboardMarkup(buttons);
        }

        public static IReplyMarkup CreateInLineQueuedMarkup(List<TorrentCreatorUploaderFile> fileList)
        {
            var buttonsList = new Dictionary<string, string>();
            foreach (var file in fileList)
            {
                if (!file.Processed)
                    buttonsList.Add(
                        $"ProcessFile#{file.Id}",
                        $"Full process: {Path.GetFileName(file.FilePath)?.Replace("_", "") ?? throw new InvalidOperationException()}");
                else
                    buttonsList.Add(
                        $"InvalidActionResponse#File already processed#{file.Id}",
                        "File already processed");
                if (!file.CreatedTorrent)
                    buttonsList.Add(
                        $"CreateTorrent#{file.Id}",
                        $"Create torrent: {Path.GetFileName(file.FilePath)?.Replace("_", "") ?? throw new InvalidOperationException()}");
                else
                    buttonsList.Add(
                        $"InvalidActionResponse#Torrent already created#{file.Id}",
                        "Torrent already created");
                if (!file.UploadedTorrent)
                    buttonsList.Add(
                        $"UploadTorrent#{file.Id}",
                        $"Upload torrent: {Path.GetFileName(file.FilePath)?.Replace("_", "") ?? throw new InvalidOperationException()}");
                else
                    buttonsList.Add(
                        $"InvalidActionResponse#Torrent already uploaded#{file.Id}",
                        "Torrent already uploaded");
                if (!file.SentTorrentToUTorrentViaWebApi)
                    buttonsList.Add(
                        $"SendToUTorrent#{file.Id}",
                        $"Send to uTorrent: {Path.GetFileName(file.FilePath)?.Replace("_", "") ?? throw new InvalidOperationException()}");
                else
                    buttonsList.Add(
                        $"InvalidActionResponse#Torrent already sent to uTorrent#{file.Id}",
                        "Torrent already sent to uTorrent");
                if (!file.SentTorrentToDelugeViaDelugeConsole)
                    buttonsList.Add(
                        $"SendToDeluge#{file.Id}",
                        $"Send to Deluge: {Path.GetFileName(file.FilePath)?.Replace("_", "") ?? throw new InvalidOperationException()}");
                else
                    buttonsList.Add(
                        $"InvalidActionResponse#Torrent already sent to Deluge#{file.Id}",
                        "Torrent already sent to Deluge");
                if (!file.CopiedTorrentFile)
                    buttonsList.Add(
                        $"CopyTorrent#{file.Id}",
                        $"Copy Torrent: {Path.GetFileName(file.FilePath)?.Replace("_", "") ?? throw new InvalidOperationException()}");
                else
                    buttonsList.Add(
                        $"InvalidActionResponse#Torrent already copied#{file.Id}",
                        "Torrent already copied");
                if (!file.DeletedTorrentFileAfterEveryThing)
                    buttonsList.Add(
                        $"DeleteTorrent#{file.Id}",
                        $"Delete Torrent: {Path.GetFileName(file.FilePath)?.Replace("_", "") ?? throw new InvalidOperationException()}");
                else
                    buttonsList.Add(
                        $"InvalidActionResponse#Torrent already deleted#{file.Id}",
                        "Torrent already deleted");
                buttonsList.Add(
                    $"MarkFileAsProcessed#{file.Id}",
                    $"Mark file as processed: {Path.GetFileName(file.FilePath)?.Replace("_", "") ?? throw new InvalidOperationException()}");
                buttonsList.Add(
                    $"NoReponse#NoReponse{file.Id}",
                    string.Format("\r\n{0}{0}{0}{0}{0}{0}{0}{0}{0}{0}", "\u2796"));
            }

            return CreateInlineKeyboardButton(buttonsList, 1);
        }

        private static string GetEmoticonBool(bool value)
        {
            return value ? "\u2714" : "\u2716";
        }

        private static string GetEmoticonInt(int value)
        {
            switch (value)
            {
                case 0:
                    return "\u0030";
                case 1:
                    return "\u0031";
                case 2:
                    return "\u0032";
                case 3:
                    return "\u0033";
                case 4:
                    return "\u0034";
                case 5:
                    return "\u0035";
                case 6:
                    return "\u0036";
                case 7:
                    return "\u0037";
                case 8:
                    return "\u0038";
                case 9:
                    return "\u0039";
                default:
                    return "\u0023";
            }
        }

        public static ReplyKeyboardMarkup CustomerKeyBoard()
        {
            var keyb = new ReplyKeyboardMarkup
            {
                Keyboard = new[]
                {
                    new[]
                    {
                        new KeyboardButton("/get_unprocessed [asc] [10]"),
                        new KeyboardButton("/get_processed [desc] [10]")
                    },
                    new[]
                    {
                        new KeyboardButton("/process_a_queued_file [asc] [10]"),
                        new KeyboardButton("/process_an_in_error_file [asc] [10]")
                    },
                    new[]
                    {
                        new KeyboardButton("/get_logs_error [asc] [10]"),
                        new KeyboardButton("/get_logs_info [asc] [10]")
                    },
                    new[]
                    {
                        new KeyboardButton("/help")
                    }
                }
            };
            return keyb;
        }
    }
}