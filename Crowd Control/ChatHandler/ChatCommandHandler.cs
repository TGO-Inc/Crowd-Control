using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using CrowdControl.Util;
using CrowdControl.Extensions;
using Newtonsoft.Json.Linq;
using smSteamUtility;
using StreamingServices.Chat;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Steamworks;

namespace CrowdControl.ChatHandler
{
    public class ChatCommandHandler : IDisposable
    {
        public Steam SteamHandler { get; set; }
        private ConcurrentQueue<KeyValuePair<ChatEventArgs, ChatCommand>> ChatCommands { get; set; }
        private ChatCommands ValidCommands { get; set; }
        private BackgroundWorker JsonPipeThread { get; set; }
        private DirectoryInfo WorkingDir { get; set; }
        private string StreamChatJson { get; set; }
        private Action PropertyChanged { get; set; }
        private string ProfileName { get; set; }
        private DirectoryInfo ProfileDir { get; set; }
        private static readonly string BaseUri = new Uri(Environment.CurrentDirectory).LocalPath;
        private bool _disposed;
        public ChatCommandHandler(Action PropertiesChanged, ChatCommands c)
        {
            this.PropertyChanged = PropertiesChanged;
            this.ProfileName = "Default";

            this.ProfileDir = new(Path.Combine(BaseUri, "profiles"));
            if (!this.ProfileDir.Exists) this.ProfileDir.Create();

            this.ChatCommands = new();
            this.SteamHandler = new();
            this.JsonPipeThread = new()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = true
            };
            this.JsonPipeThread.DoWork += JsonPipe;

            this.ValidCommands = c;

            foreach (ChatCommand e in this.ValidCommands.Commands)
                e.RegisterOnPropertyChangedCallback(this.PropertyChanged);

            if (!this.SteamHandler.ConnectToSteam())
                throw new Exception("Could not connect to steam");

            if (!this.SteamHandler.ConnectToGame())
                throw new Exception("Could not link to scrap mechanic");

            this.WorkingDir = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Axolot Games", "Scrap Mechanic", "User", "User_76561198299556567", "Mods", "SM-Crowd-Control-II"));
            this.StreamChatJson = Path.Combine(this.WorkingDir.FullName, "Scripts", "StreamReaderData", "streamchat.json");

            this.JsonPipeThread.RunWorkerAsync();
        }
        public List<ChatCommand> GetValidCommands() => this.ValidCommands.Commands;
        private void JsonPipe(object? sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgw = (sender as BackgroundWorker)!;
            while (!bgw.CancellationPending)
            {
                if (this.ChatCommands.Count > 0)
                {
                    while (this.ChatCommands.TryDequeue(out var res))
                    {
                        string s = File.ReadAllText(this.StreamChatJson).Trim();
                        if (s.Equals("null")) s = "[]";
                        if (s.Equals("\"{}\"")) s = "[]";
                        JArray json = JArray.Parse(s);
                        FormatAndAppend(res, ref json);
                        File.WriteAllText(this.StreamChatJson, json.ToString());
                    }
                }
                Thread.Sleep(100);
            }
            e.Cancel = true;
        }
        private static void FormatAndAppend(KeyValuePair<ChatEventArgs, ChatCommand> _j, ref JArray arry)
        {
            JObject j = new()
            {
                ["command"] = _j.Value.Command.ToLower(),
                ["id"] = _j.Key.ChatId,
                ["params"] = _j.Value.Arguments.ToJToken(),
                ["username"] = _j.Key.Author.Name,
                ["amount"] = /*string.Join(" ", */_j.Key.DontationAmount/*, _j.Key.DontationType)*/
            };
            arry.Add(j);
        }
        public bool ParseCommand(ChatEventArgs e)
        {
            string message = e.Message;
            if (this.ValidCommands.Prefix.Contains(message[0].ToString()))
            {
                message = message[1..];
                string[] message_peices = message.Split(' ');
                foreach (var ValidCommand in this.ValidCommands.Commands)
                {
                    if (ValidCommand.Enabled)
                    {
                        if (ValidateCommand(ValidCommand, e, message_peices, out ChatCommand? command))
                        {
                            //if ((DateTime.Now - command.LastCall).Seconds > command.Timeout)
                            {
                                // command.LastCall = DateTime.Now;
                                if (command.ValidCommands[0].Equals("import"))
                                    this.Import(message_peices[1], e, command);
                                else
                                    this.ChatCommands.Enqueue(new(e, command));
                                break;
                            }
                        }
                    }
                }
            }
            return false;
        }
        private bool ValidateCommand(ChatCommand start, ChatEventArgs e, string[] message_peices, out ChatCommand? command) 
        {
            string user_command = message_peices[0].ToLower();
            if (start.ValidCommands is null || start.ValidCommands.Count == 0 || start.ValidCommands.Contains(user_command))
            {
                if (e.DontationAmount >= start.MemberOrPay.Price
                    && e.Author.IsMember == start.MemberOrPay.MemberOnly
                    && e.Author.MemberDuration.TotalDays >= start.MemberOrPay.MemberDuration)
                {
                    command = new()
                    {
                        Command = user_command,
                        Enabled = start.Enabled,
                        Timeout = start.Timeout,
                        CommandType = start.CommandType,
                        ValidCommands = new() { user_command },
                        MemberOrPay = start.MemberOrPay
                    };
                    if (start.Arguments is not null && message_peices.Length > 1)
                    {
                        foreach (var arg in start.Arguments)
                        {
                            if (ValidateCommand(arg, e, message_peices[1..], out ChatCommand? out_arg))
                            {
                                if (out_arg is not null) command.Arguments.Add(out_arg);
                                return true;
                            }
                        }
                        command = null;
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            command = null;
            return false;
        }
        private void Import(string workshop_id, ChatEventArgs e, ChatCommand command)
        {
            Task.Run(() =>
            {
                bool ImportReady = false;
                bool RetryImport = false;
                string ImportDirectory;
                Callback<DownloadItemResult_t> ImportCallback;
                var workshop_item_id = new PublishedFileId_t(ulong.Parse(workshop_id));
            TryImport:
                bool k = SteamUGC.GetItemInstallInfo(workshop_item_id, out ulong sizeOnDisk, out ImportDirectory, 99999999, out uint timeStamp);
                if (!k || !Directory.Exists(ImportDirectory))
                {
#if DEBUG
                    Debug.WriteLine("Downloading...");
#endif
                    if (!RetryImport)
                    {
#if DEBUG
                        Debug.WriteLine("Trying download...");
#endif
                        Directory.CreateDirectory(ImportDirectory);
                        ImportCallback = Callback<DownloadItemResult_t>.Create((DownloadItemResult_t param) =>
                        {
                            if (!ImportReady)
                            {
                                ImportReady = true;
                                SteamUGC.GetItemInstallInfo(param.m_nPublishedFileId, out ulong sizeOnDisk, out string Folder, 99999999, out uint timeStamp);
                                ImportDirectory = Folder;
                                Debug.WriteLine($"Downloaded to: {Folder}");
                            }
                        });
                        SteamUGC.DownloadItem(workshop_item_id, true);
                        while (!ImportReady)
                        {
                            SteamAPI.RunCallbacks();
                            Thread.Sleep(100);
                        }
                        ImportCallback.Unregister();
                        RetryImport = true;
                        goto TryImport;
                    }
                    else
                    {
#if DEBUG
                        Debug.WriteLine("Failed to download item");
#endif
                        return;
                    }
                }
                else
                {
#if DEBUG
                    Debug.WriteLine("Already downloaded");
#endif
                }
                this.VerifyDownload(workshop_item_id, new(ImportDirectory), e, command);
            });
        }
        private bool VerifyDownload(PublishedFileId_t workshop_id, DirectoryInfo folder, ChatEventArgs e, ChatCommand command)
        {
            string desc = Path.Combine(folder.FullName, "description.json");
            string blueprint = Path.Combine(folder.FullName, "blueprint.json");
            if (File.Exists(desc) && File.Exists(blueprint))
            {
                JObject Info = JObject.Parse(File.ReadAllText(desc));
                var lvl1Args = command.Arguments[0].Arguments;
                if (lvl1Args.Count > 0)
                {
                    string argv;
                    if (lvl1Args[0].Arguments.Count > 0)
                        argv = lvl1Args[0].Arguments[0].Command.ToString();
                    else
                        argv = lvl1Args[0].Command.ToString();

                    ChangeBlueprintState(blueprint, Convert.ToInt32(argv.Equals("static")));
                }
                DirectoryInfo download_folder = new(Path.Combine(this.WorkingDir.FullName, "tmp-download"));
                if (!download_folder.Exists) download_folder.Create();
                File.Copy(blueprint, Path.Combine(download_folder.FullName, "blueprint.json"), true);
                File.Copy(desc, Path.Combine(download_folder.FullName, "description.json"), true);
                this.ChatCommands.Enqueue(new(e, command));
                return true;
            }
            else
            {
#if DEBUG
                Debug.WriteLine("Not valid blueprint?");
                Debug.WriteLine(folder.FullName);
                Debug.WriteLineIf(!File.Exists(blueprint), "Blueprint not found");
                Debug.WriteLineIf(!File.Exists(desc), "Description not found");
                folder.Delete(true);
#endif
            }
            return false;
        }
        private static void ChangeBlueprintState(string file, int state)
        {
            JObject json = JObject.Parse(File.ReadAllText(file));
            JArray bodies = json["bodies"] as JArray;
            foreach(var item in bodies)
                item["type"] = state;
            File.WriteAllText(file, json.ToString());
        }
        private static void CenterBlurpint()
        {

        }
        public void SetProfileName(string n)
        {
            this.ProfileName = n;
            if (this.ProfileName.EndsWith('*'))
                this.ProfileName = this.ProfileName[..(this.ProfileName.Length - 1)];
        }
        public void ReOrderCommand(ChatCommand command, int NewIndex)
        {
            this.ReOrderCommand(this.ValidCommands.Commands.IndexOf(command), NewIndex);
        }
        public void ReOrderCommand(int OldIndex, int NewIndex)
        {
            if (OldIndex >= 0)
            {
                var oldCommand = this.ValidCommands.Commands[OldIndex];
                this.ValidCommands.Commands.RemoveAt(OldIndex);
                this.ValidCommands.Commands.Insert(NewIndex, oldCommand);
            }
        }
        public void SaveToFile()
        {
            if (!this.ProfileName.Equals("Default")) File.WriteAllText(Path.Combine(this.ProfileDir.FullName, this.ProfileName + ".sm.config.json"), JsonConvert.SerializeObject(this.ValidCommands));
        }
        public void DeleteFile()
        {
            string f = Path.Combine(this.ProfileDir.FullName, this.ProfileName + ".sm.config.json");
            if (File.Exists(f)) File.Delete(f);
        }
        public static ChatCommandHandler LoadFromFile(Action PropertiesChanged, string ProfileName = "Default")
        {
            string name = ProfileName;
            if (name.EndsWith('*'))
                name = name[..(name.Length - 1)];
            name += ".sm.config.json";
            DirectoryInfo ProfileDir = new(Path.Combine(BaseUri, "profiles"));
            if (!ProfileDir.Exists) ProfileDir.Create();
            if (File.Exists(Path.Combine(ProfileDir.FullName, name)))
            {
                
                string content = File.ReadAllText(Path.Combine(ProfileDir.FullName, name));
                try
                {
                    return new(PropertiesChanged, JsonConvert.DeserializeObject<ChatCommands>(content)!);
                }
                catch
                {
                    return new(PropertiesChanged, Utility.LoadInternalFile.JsonFile("Default.sm.config.json"));
                }
            }
            else
            {
                try
                {
                    return new(PropertiesChanged, Utility.LoadInternalFile.JsonFile(name));
                }
                catch
                {
                    return new(PropertiesChanged, Utility.LoadInternalFile.JsonFile("Default.sm.config.json"));
                }
            }
        }
        public List<string> LoadProfiles()
        {
            List<string> list = new();
            foreach (FileInfo f in this.ProfileDir.GetFiles())
            {
                if (f.Name.EndsWith(".sm.config.json"))
                {
                    list.Add(f.Name[..(f.Name.Length - 15)]);
                }
            }
            return list;
        }
        ~ChatCommandHandler()
        {
            if (!_disposed)
                Dispose(true);
            else
                Dispose(false);
        }
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                this.JsonPipeThread.CancelAsync();
                while (this.JsonPipeThread.IsBusy) { }
                this.JsonPipeThread.Dispose();
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            _disposed = true;
        }
        public void Test()
        {
            string id = "2844990594";
            this.Import(id, new(
                $"!import {id} above dynamic",
                Guid.NewGuid().ToString(),
                new("TheGuy920", "123456789",
                false,
                0,
                false,
                TimeSpan.Zero),
                0,
                DonationType.None,
                false,
                0),
            new()
            {
                Command = "import",
                MemberOrPay = new(),
                CommandType = CommandType.Base,
                Enabled = true,
                Timeout = 0,
                ValidCommands = new() { "import" },
                Arguments = new()
                {
                    new()
                    {
                        Command = id,
                        MemberOrPay = new(),
                        CommandType = CommandType.Optional,
                        Enabled = true,
                        Timeout = 0,
                        ValidCommands = new() { },
                        Arguments = new()
                        {
                            new()
                            {
                                Command = "above",
                                MemberOrPay = new(),
                                CommandType = CommandType.Optional,
                                Enabled = true,
                                Timeout = 0,
                                ValidCommands = new() { "above" },
                                Arguments = new()
                                {
                                    new()
                                    {
                                        Command = "dynamic",
                                        MemberOrPay = new(),
                                        CommandType = CommandType.Optional,
                                        Enabled = true,
                                        Timeout = 0,
                                        ValidCommands = new() { "dynamic" },
                                        Arguments = new()
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }
    }
}
