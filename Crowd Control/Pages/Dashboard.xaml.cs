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
using System.Windows.Input;
using CrowdControl.Windows;
using System.IO;

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
        private TimerView? TimersWindow { get; set; }
        private ChatReader? Reader {  get; set; }
        #region CardHandler
        private Card? MovingCard;
        private System.Windows.Point Offset;
        private int MovingCardIndex;
        private int StartIndex;
        #endregion
        public Dashboard()
        {
            InitializeComponent();
            
            this.ChatCommandHandler = ChatCommandHandler.LoadFromFiles(PropertiesChanged, new[] { "Default", "SaltySeraph" });

            if (this.ChatCommandHandler is null)
                throw new Exception("Failed to load commands");

            foreach (ChatCommand e in this.ChatCommandHandler.GetValidCommands())
                this.BuildCards(e);

            foreach (string profile in this.ChatCommandHandler.LoadProfiles())
                this.ProfileComboBox.Items.Insert(this.ProfileComboBox.Items.Count - 1, new ComboBoxItem() { Content = profile });

            this.SteamName.Text = this.ChatCommandHandler.SteamHandler.UserName;
#if !DEBUG
            this.ChatCommandHandler.SteamHandler.LoadAvatars();
            this.Avatar.Source = ImageSourceFromBitmap(this.ChatCommandHandler.SteamHandler.LargeAvatar);
#endif
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
            Wpf.Ui.Controls.TextBox a = ((VisualTreeHelper.GetParent(VisualTreeHelper.GetParent((DependencyObject)sender)) as Grid)!.Children[0] as Wpf.Ui.Controls.TextBox)!;
            this.ReadChat(a?.Text ?? "");
        }
        private void ReadChat(string video_id)
        {
            this.Reader?.Dispose();

            this.Reader = new();
            this.Reader.ChatLoaded += ChatLoaded;
            this.Reader.ChatEvent += ChatEvent;
            this.Reader.AddYoutubeStream(video_id);
            this.StreamStatus.ToolTip = "Connecting...";
            this.StreamStatus.Child = new ProgressRing()
            {
                IsIndeterminate = true,
                LayoutTransform = new ScaleTransform() { ScaleX = 0.3, ScaleY = 0.3 }
            };
        }
        private void ChatEvent(ChatEventArgs e)
        {
            if (this.CanReadChat)
            {
                Debug.WriteLine(e.Message);
                this.ChatCommandHandler.ParseCommand(e);
            }
        }
        private void ChatLoaded(ChatLoadedArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.StreamStatus.ToolTip = "Connected to Stream";
                this.StreamStatus.Child = new SymbolIcon()
                {
                    Symbol = Wpf.Ui.Common.SymbolRegular.CheckboxChecked24,
                    FontSize = 20,
                    IsEnabled = false,
                    Foreground = Brushes.Green
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

            if (item!.Tag is not null)
            {
                if (item.Tag.ToString()!.Equals("Default"))
                {
                    List<string> t = new();
                    foreach (var p in this.ProfileComboBox.Items)
                        t.Add((p as ComboBoxItem)!.Content.ToString()!);
                    string? profile_name = new NewProfileWindow(t).CreateNewProfile();
                    if (profile_name is not null)
                    {
                        this.ProfileComboBox.Items.Insert(this.ProfileComboBox.Items.Count - 1, new ComboBoxItem() { Content = profile_name + "*" });
                        this.ProfileComboBox.SelectedIndex = this.ProfileComboBox.Items.Count - 2;
                        return;
                    }
                }
            }

            string s = item.Content.ToString()!;
            if (s.EndsWith("*")) item.Content = s[..(s.Length - 1)];
            this.ChatCommandHandler.SetProfileName(item.Content.ToString()!);
            this.ChatCommandHandler.SaveToFile();
        }
        private void BuildCards(ChatCommand e)
        {
            Card c = e.ToCard();
            c.MouseDown += Card_MouseDown;
            c.MouseUp += Card_MouseUp;
            this.CommandWrap.Children.Add(c);
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
            string s = (this.ProfileComboBox.SelectedItem as ComboBoxItem)!.Content.ToString()!;
            
            if (this.TimersWindow is not null)
                this.TimersWindow.ClearTimers();

            Task.Factory.StartNew(async () =>
            {
                ReadOnlyCollection<ChatCommand> l;
                lock (this.ChatCommandHandler)
                {
                    this.ChatCommandHandler = ChatCommandHandler.LoadFromFile(PropertiesChanged, s);
                    this.Dispatcher.Invoke(() => { this.CommandWrap.Children.Clear(); });
                    this.ChatCommandHandler.SetProfileName(s);
                    l = this.ChatCommandHandler.GetValidCommands().AsReadOnly();
                }
                foreach (ChatCommand k in l)
                {
                    var t = this.CommandWrap.Dispatcher.BeginInvoke(() => { this.BuildCards(k); });
                    t.Priority = System.Windows.Threading.DispatcherPriority.Background;
                    if (this.TimersWindow is not null)
                        this.TimersWindow.AddTimer(k.Command, k.Timeout, k.Enabled);
                    await Task.Delay(100);
                }
                await Task.Delay(100);
                await this.Dispatcher.BeginInvoke(() =>
                {
                    this.LoadProfileButton.Visibility = Visibility.Visible;
                    this.LoadingProfile.Visibility = Visibility.Collapsed;
                    if (prev_state) this.EnableChatReader(null, null);
                    this.EnableReadChat.IsEnabled = true;
                });
                await Task.Delay(250);
                this.IsLoadingProfile = false;
            });
        }
        private void ChangeProfile(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = e.AddedItems[0] as ComboBoxItem;
                if (item!.Tag is not null)
                {
                    if (item.Tag.Equals("New"))
                    {
                        this.ProfileComboBox.SelectedItem = e.RemovedItems[0];
                        List<string> s = new();
                        foreach (var p in this.ProfileComboBox.Items)
                            s.Add((p as ComboBoxItem)!.Content.ToString()!);
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
                    if (item!.Tag is not null) if (item.Tag.ToString()!.Equals("Default")) return;
                    string s = item.Content.ToString()!;
                    if (!s.EndsWith("*")) item.Content = s + "*";
                }
            });

            if (this.TimersWindow is null)
                return;

            foreach (var command in this.ChatCommandHandler.GetValidCommands())
                if (command.Enabled)
                    this.TimersWindow.EnableTimer(command.Command);
                else
                    this.TimersWindow.DisableTimer(command.Command);
        }
        private void DeleteProfile(object sender, RoutedEventArgs e)
        {
            var item = this.ProfileComboBox.SelectedItem as ComboBoxItem;
            if (item!.Tag is not null) if (item.Tag.Equals("New") || item.Tag.Equals("Default")) return;
            if (new ConfirmDelete().ConfirmDeletion())
            {
                int ind = this.ProfileComboBox.SelectedIndex - 1;
                this.ChatCommandHandler.SetProfileName(item.Content.ToString()!);
                this.ChatCommandHandler.DeleteFile();
                this.ProfileComboBox.Items.Remove(item);
                this.ProfileComboBox.SelectedIndex = ind;
            }
        }
        private void EnableChatReader(object sender, RoutedEventArgs e)
        {
            this.GameConnectStatus.Visibility = Visibility.Collapsed;
            //this.GameConnectStatus.Symbol = Wpf.Ui.Common.SymbolRegular.CheckboxChecked24;
            //this.GameConnectStatus.Foreground = Brushes.Green;
            this.GameConnectLoading.Visibility = Visibility.Visible;
            this.CanReadChat = true;
        }
        private void DisableChatReader(object sender, RoutedEventArgs e)
        {
            this.GameConnectStatus.Symbol = Wpf.Ui.Common.SymbolRegular.ErrorCircle24;
            this.GameConnectStatus.Foreground = Brushes.Red;
            this.GameConnectStatus.Visibility = Visibility.Visible;
            this.GameConnectLoading.Visibility = Visibility.Collapsed;
            this.CanReadChat = false;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.ChatCommandHandler.Test(this.TestCommandField.Text);
        }
        private void Card_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.MovingCard is null)
            {
                this.MovingCard = (sender as Card)!;
                this.MovingCardIndex = this.CommandWrap.Children.IndexOf(this.MovingCard) + 1;
                this.StartIndex = this.MovingCardIndex - 1;
                this.Offset = e.MouseDevice.GetPosition(this.MovingCard);
                if (this.MovingCardShadow.Parent != null)
                    (this.MovingCardShadow.Parent as Panel)!.Children.Remove(this.MovingCardShadow);
                this.MovingCardShadow.Visibility = Visibility.Visible;

                var b = (this.MovingCardShadow.Content as Border)!;
                b.Height = this.MovingCard.ActualHeight - 35;
                b.Width = this.MovingCard.ActualWidth - 30;

                var p = (this.MovingCard.Content as Panel);
                p!.Width = p.ActualWidth;

                this.CommandWrap.Children.Insert(this.MovingCardIndex, this.MovingCardShadow);
                this.CommandWrap.Children.Remove(this.MovingCard);

                this.MovingCard.HorizontalAlignment = HorizontalAlignment.Left;
                this.MovingContainer.Children.Add(this.MovingCard);
            }
            else
            {
                this.Card_MouseUp(sender, e);
            }
        }
        private void Card_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (this.MovingCard is not null)
            {
                this.MovingCardShadow.Visibility = Visibility.Collapsed;

                this.MovingContainer.Children.Remove(this.MovingCard);
                this.MovingCard!.Margin = new(20, 20, 0, 0);
                this.MovingCard.HorizontalAlignment = HorizontalAlignment.Stretch;
                (this.MovingCard.Content as Panel)!.Width = Double.NaN;
                int ind = this.CommandWrap.Children.IndexOf(this.MovingCardShadow);
                this.CommandWrap.Children.Insert(ind, this.MovingCard);
                this.CommandWrap.Children.Remove(this.MovingCardShadow);
                this.CommandWrap.Children.Add(this.MovingCardShadow);
                this.ChatCommandHandler.ReOrderCommand(this.StartIndex, ind);
                this.MovingCard = null;
                this.PropertiesChanged();
            }
        }
        public void InvokeMouseUp(MouseButtonEventArgs e)
        {
            this.Card_MouseUp(null, e);
        }
        public void InvokeMouseMove(MouseEventArgs e)
        {
            if (this.MovingCard is not null)
            {
                var off = e.GetPosition(this.MovingCard);

                double horiz_count = Math.Floor(this.CommandWrap.ActualWidth / this.MovingCard.ActualWidth);
                double horiz_pos = Math.Floor((this.MovingCard.Margin.Left + (this.MovingCard.ActualWidth / 2)) / this.MovingCard.ActualWidth);

                var sv = (this.CommandWrap.Parent as ScrollViewer);
                // double vert_count = Math.Floor(sv!.ActualHeight / this.MovingCard.ActualHeight);
                double vert_pos = Math.Floor(this.MovingCard.Margin.Top / this.MovingCard.ActualHeight);
                vert_pos += Math.Floor(sv!.VerticalOffset / this.MovingCard.ActualHeight);
                var npos = (vert_pos * horiz_count) + horiz_pos;
                if (this.MovingCardShadow.Tag != npos.ToString())
                {
                    this.CommandWrap.Children.Remove(this.MovingCardShadow);
                    if (npos > this.CommandWrap.Children.Count)
                    {
                        this.CommandWrap.Children.Add(this.MovingCardShadow);
                    }
                    else
                    {
                        this.CommandWrap.Children.Insert((int)npos, this.MovingCardShadow);
                    }
                    this.MovingCardShadow.Tag = npos.ToString();
                }

                var diff = off - this.Offset;
                var nx = Math.Max(0, this.MovingCard.Margin.Left + diff.X);
                var ny = Math.Max(0, this.MovingCard.Margin.Top + diff.Y);
                nx = Math.Min(this.MovingContainer.ActualWidth - this.MovingCard.ActualWidth, nx);
                var bottom = this.MovingContainer.ActualHeight - (this.MovingCard.ActualHeight + 1);
                ny = Math.Min(bottom, ny);

                if (bottom - ny < 5)
                {
                    //var sv = (this.CommandWrap.Parent as ScrollViewer);
                    sv!.ScrollToVerticalOffset(sv.VerticalOffset + 1);
                    ny = bottom - 6;
                }
                if (ny < 5)
                {
                    //var sv = (this.CommandWrap.Parent as ScrollViewer);
                    sv!.ScrollToVerticalOffset(sv.VerticalOffset - 1);
                    ny = 6;
                }

                this.MovingCard.Margin = new(nx, ny, 0, 0);
            }
        }

        private void PopoutTimers(object sender, RoutedEventArgs e)
        {
            if (this.TimersWindow is null)
            {
                Dictionary<string, int> timerValues = new();
                foreach (var command in this.ChatCommandHandler.GetValidCommands())
                    timerValues.Add(command.Command.ToLower().Trim(), command.Timeout);
                
                this.TimersWindow = new(timerValues);
                this.TimersWindow.Show();
                this.TimersWindow.Closed += (arg1, arg2) =>
                {
                    this.TimersWindow = null;
                };
            }
            else
            {
                this.TimersWindow.ResetRandomTimer();
            }
        }

        private void EnableReadChat_Click(object sender, RoutedEventArgs e)
        {
            if (this.CanReadChat)
                this.DisableChatReader(sender, e);
            else
                this.EnableChatReader(sender, e);
        }

        private void LaunchGame(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psi = new()
            {
                FileName = Path.Combine(this.ChatCommandHandler.SteamHandler.GameDirectory.FullName, ".", "Release", "ScrapMechanic.exe"),
                WorkingDirectory = Path.Combine(this.ChatCommandHandler.SteamHandler.GameDirectory.FullName, ".", "Release"),
                Arguments = "-dev --ugc 0ad813dd-f9e5-4ccb-9cec-15fcbab86289"
            };
            Process.Start(psi);
        }
    }
}