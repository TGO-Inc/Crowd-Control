using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using StreamingServices;
using StreamingServices.Chat;
using CrowdControl.ChatHandler;
using System.Runtime.InteropServices;
using System;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Brushes = System.Windows.Media.Brushes;
using System.Collections.ObjectModel;

namespace CrowdControl.Pages
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : UiPage
    {
        private ChatCommandHandler ChatCommandHandler { get; set; }
        private bool ShowInfo = false;
        private bool IsLoadingProfile = true;
        private bool CanReadChat = false;
        public Dashboard()
        {
            InitializeComponent();

            this.ChatCommandHandler = ChatCommandHandler.LoadFromFile(PropertiesChanged, "Default");

            if (this.ChatCommandHandler is null)
                throw new Exception("Failed to load commands");

            foreach (ChatCommand e in this.ChatCommandHandler.GetValidCommands())
                this.CommandWrap.Children.Add(e);

            foreach(string profile in this.ChatCommandHandler.LoadProfiles())
                this.ProfileComboBox.Items.Insert(this.ProfileComboBox.Items.Count - 1, new ComboBoxItem() { Content = profile });

            this.SteamName.Text = this.ChatCommandHandler.SteamHandler.UserName;
            this.ChatCommandHandler.SteamHandler.LoadAvatars();
            this.Avatar.Source = ImageSourceFromBitmap(this.ChatCommandHandler.SteamHandler.LargeAvatar);

            this.IsLoadingProfile = false;
        }
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);
        public static ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
        private void UpdateUrl(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Controls.TextBox a = (VisualTreeHelper.GetParent(VisualTreeHelper.GetParent((DependencyObject)sender)) as Grid).Children[0] as Wpf.Ui.Controls.TextBox;
            this.ReadChat(a?.Text ?? "");
        }
        private void ReadChat(string video_id)
        {
            ChatReader chatreader = new();
            chatreader.ChatLoaded += ChatLoaded;
            chatreader.ChatEvent += ChatEvent;
            chatreader.AddYoutubeStream(video_id);
            this.StreamStatus.ToolTip = "Connecting...";
            this.StreamStatus.Child = new ProgressRing() 
            {
                IsIndeterminate = true,
                LayoutTransform = new ScaleTransform() { ScaleX = 0.3, ScaleY = 0.3 }
            };
        }
        private void ChatEvent(ChatEventArgs e)
        {
            Debug.WriteLine(e.Message);
            if(this.CanReadChat) this.ChatCommandHandler.ParseCommand(e);
        }
        private void ChatLoaded(ChatLoadedArgs e)
        {
            this.Dispatcher.Invoke(() => {
                this.StreamStatus.ToolTip = "Connected to Stream";
                this.StreamStatus.Child = new SymbolIcon() { 
                    Symbol = Wpf.Ui.Common.SymbolRegular.CheckboxChecked24,
                    FontSize = 20,
                    IsEnabled = false,
                    Foreground = System.Windows.Media.Brushes.Green
                };
            });
        }
        private void StreamInfoToggle(object sender, RoutedEventArgs e)
        {
            this.StreamInfo.IsExpanded = (this.ShowInfo = !this.ShowInfo);
        }
        private void SaveProfile(object sender, RoutedEventArgs e)
        {
            var item = this.ProfileComboBox.SelectedItem as ComboBoxItem;
            
            if (item.Tag is not null)
            {
                if (item.Tag.ToString().Equals("Default"))
                {
                    List<string> t = new();
                    foreach (var p in this.ProfileComboBox.Items)
                        t.Add((p as ComboBoxItem).Content.ToString());
                    string? profile_name = new NewProfileWindow(t).CreateNewProfile();
                    if (profile_name is not null)
                    {
                        this.ProfileComboBox.Items.Insert(this.ProfileComboBox.Items.Count - 1, new ComboBoxItem() { Content = profile_name + "*" });
                        this.ProfileComboBox.SelectedIndex = this.ProfileComboBox.Items.Count - 2;
                        return;
                    }
                }
            }

            string s = item.Content.ToString();
            if (s.EndsWith("*")) item.Content = s[..(s.Length-1)];
            this.ChatCommandHandler.SetProfileName(item.Content.ToString());
            this.ChatCommandHandler.SaveToFile();
        }
        private void LoadProfile(object sender, RoutedEventArgs e)
        {
            bool prev_state = this.CanReadChat;
            this.Dispatcher.BeginInvoke(() =>
            {
                this.DisableChatReader(null, null);
                this.EnableReadChat.IsEnabled = false;
                this.IsLoadingProfile = true;
                this.LoadProfileButton.Visibility = Visibility.Collapsed;
                this.LoadingProfile.Visibility = Visibility.Visible;
            });
            string s = (this.ProfileComboBox.SelectedItem as ComboBoxItem).Content.ToString();
            Task.Factory.StartNew(() =>
            {
                ReadOnlyCollection<ChatCommand> l;
                lock (this.ChatCommandHandler)
                {
                    this.ChatCommandHandler = ChatCommandHandler.LoadFromFile(PropertiesChanged, s);
                    this.Dispatcher.Invoke(()=> { this.CommandWrap.Children.Clear(); });
                    this.ChatCommandHandler.SetProfileName(s);
                    l = this.ChatCommandHandler.GetValidCommands().AsReadOnly();
                }
                foreach (ChatCommand k in l)
                {
                    var t = this.CommandWrap.Dispatcher.BeginInvoke(() => { this.CommandWrap.Children.Add(k); });
                    t.Priority = System.Windows.Threading.DispatcherPriority.Background;
                    Task.Delay(75).Wait();
                }
                Task.Delay(100).Wait();
                this.Dispatcher.BeginInvoke(() =>
                {
                    this.LoadProfileButton.Visibility = Visibility.Visible;
                    this.LoadingProfile.Visibility = Visibility.Collapsed;
                    if(prev_state) this.EnableChatReader(null, null);
                    this.EnableReadChat.IsEnabled = true;
                }).Wait();
                Task.Delay(250).Wait();
                this.IsLoadingProfile = false;
            });
        }
        private void ChangeProfile(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as ComboBoxItem;
                if (item.Tag is not null)
                {
                    if (item.Tag.Equals("New"))
                    {
                        this.ProfileComboBox.SelectedItem = e.RemovedItems[0];
                        List<string> s = new();
                        foreach (var p in this.ProfileComboBox.Items)
                            s.Add((p as ComboBoxItem).Content.ToString());
                        string? profile_name = new NewProfileWindow(s).CreateNewProfile();
                        if (profile_name is not null)
                        {
                            this.ProfileComboBox.Items.Insert(this.ProfileComboBox.Items.Count - 1, new ComboBoxItem() { Content = profile_name + "*" });
                            this.ProfileComboBox.SelectedIndex = this.ProfileComboBox.Items.Count - 2;
                        }
                    }
                }
            }
        }
        private void PropertiesChanged()
        {
            this.Dispatcher.Invoke(() =>
            {
                if (!this.IsLoadingProfile)
                {
                    var item = this.ProfileComboBox.SelectedItem as ComboBoxItem;
                    if (item.Tag is not null) if (item.Tag.ToString().Equals("Default")) return;
                    string s = item.Content.ToString();
                    if (!s.EndsWith("*")) item.Content = s + "*";
                }
            });
        }
        private void DeleteProfile(object sender, RoutedEventArgs e)
        {
            var item = this.ProfileComboBox.SelectedItem as ComboBoxItem;
            if (item.Tag is not null) if (item.Tag.Equals("New") || item.Tag.Equals("Default")) return;
            if (new ConfirmDelete().ConfirmDeletion())
            {
                int ind = this.ProfileComboBox.SelectedIndex - 1;
                this.ChatCommandHandler.SetProfileName(item.Content.ToString());
                this.ChatCommandHandler.DeleteFile();
                this.ProfileComboBox.Items.Remove(item);
                this.ProfileComboBox.SelectedIndex = ind;
            }
        }
        private void EnableChatReader(object sender, RoutedEventArgs e)
        {
            (this.EnableReadChat.Content as SymbolIcon).Symbol = Wpf.Ui.Common.SymbolRegular.CheckboxChecked24;
            (this.EnableReadChat.Content as SymbolIcon).Foreground = Brushes.Green;
            this.CanReadChat = true;
        }
        private void DisableChatReader(object sender, RoutedEventArgs e)
        {
            (this.EnableReadChat.Content as SymbolIcon).Symbol = Wpf.Ui.Common.SymbolRegular.ErrorCircle24;
            (this.EnableReadChat.Content as SymbolIcon).Foreground = Brushes.Red;
            this.CanReadChat = false;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.ChatCommandHandler.Test();
        }
    }
}