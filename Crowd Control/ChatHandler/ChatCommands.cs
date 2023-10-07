using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamingServices.Chat;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CrowdControl.Extensions;
using Wpf.Ui.Controls;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Input;
using System;
using System.Linq;
using System.Threading.Tasks;
using TextBox = Wpf.Ui.Controls.TextBox;
using Newtonsoft.Json.Converters;

namespace CrowdControl.ChatHandler
{
    public class MemberOrPay
    {
        [JsonProperty("price")]
        public double Price { get; set; }

        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("donation_type")]
        public DonationType DonationType { get; set; }

        [JsonProperty("member_only")]
        public bool MemberOnly { get; set; }

        [JsonProperty("member_duration")]
        public int MemberDuration { get; set; }
    }
    public class ChatCommand
    {
        [JsonProperty("command")]
        public string Command { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("commandTag")]
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public CommandType CommandType { get; set; }

        [JsonProperty("commandNames")]
        public List<string> ValidCommands { get; set; }
        
        [JsonProperty("timeout")]
        public int Timeout { get; set; }

        [JsonProperty("member_or_pay")]
        public MemberOrPay MemberOrPay { get; set; }

        [JsonProperty("arguments")]
        public List<ChatCommand> Arguments { get; set; }
        

        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public DateTime LastCall;
        private Card Self;
        private bool InitSelf = false;
        private int SelfLevel = 0;
        private Action? callback;
        private static readonly Dictionary<Key, char> NumberKeys = new()
        {
            { Key.D0, '0' },
            { Key.D1, '1' },
            { Key.D2, '2' },
            { Key.D3, '3' },
            { Key.D4, '4' },
            { Key.D5, '5' },
            { Key.D6, '6' },
            { Key.D7, '7' },
            { Key.D8, '8' },
            { Key.D9, '9' },
            { Key.NumPad0, '0' },
            { Key.NumPad1, '1' },
            { Key.NumPad2, '2' },
            { Key.NumPad3, '3' },
            { Key.NumPad4, '4' },
            { Key.NumPad5, '5' },
            { Key.NumPad6, '6' },
            { Key.NumPad7, '7' },
            { Key.NumPad8, '8' },
            { Key.NumPad9, '9' },
            { Key.OemPeriod, '.' },
            { Key.Decimal, '.' },
            { Key.OemComma, ',' }
        };
        private static readonly Key[] IndexResetKeys = new Key[]
        {
            Key.Delete,
            Key.Back
        };
        private readonly string DecimalRegex = @"^\-?(\d+(?:[\.\,]|[\.\,]\d+)?)?$";
        private readonly string IntRegex = @"[0-9]+";
        //private string 

        [Newtonsoft.Json.JsonConstructor]
        public ChatCommand()
        {
            this.Command = string.Empty;
            this.MemberOrPay = new();
            this.Arguments = new();
            this.ValidCommands = new();
            Application.Current.Dispatcher.Invoke(() => { this.Self = new(); });
        }
        public override string ToString()
        {
            if (this.Arguments.Count > 0)
                return string.Join("\", \"", this.Command, this.Arguments.ToString(false));
            else
                return this.Command;
        }
        public string ToString(bool normal = true)
        {
            if (normal)
                return System.Text.Json.JsonSerializer.Serialize(this);
            else if (this.Arguments.Count > 0)
                return string.Join("\", \"", this.Command, this.Arguments.ToString(false));
            else
                return this.Command;
        }
        public JObject ToJObject() => JObject.FromObject(this);
        public static implicit operator UIElement(ChatCommand e)
        {
            if (e.InitSelf)
                return e.Self;
            else
                return e.ToCard();
        }
        public Card ToCard(int level = 0)
        {
            if(level > 0)
                this.SelfLevel = level;
            Thickness StackMargin = new(0);
            Thickness S2tackMargin = new(0);
            Thickness ArgMargin = new(0, 15, 0, 0);
            Thickness BaseMargin = new(20,20,0,0);
            double BaseWidth = 300;
            if (this.SelfLevel > 0)
            {
                BaseMargin = new(0, 15, 0, -5);
                S2tackMargin = new(10);
                StackMargin = new(-15);
                BaseWidth = double.NaN;
            }
            Rectangle Spacer = new()
            {
                Height = 1,
                Fill = Brushes.DarkGray,
                Margin = new(10)
            };
            StackPanel MainStack = new() { Margin = StackMargin };
            DockPanel CommandInfo = new() { Margin = S2tackMargin };
            CommandInfo.Children.Add(new TextBlock() { Text = (this.SelfLevel + 1).ToString(), Margin = new(10, 10, 0, 0) });
            TextBlock CommandName = new()
            {
                Text = this.Command.CapitilzeFirst(),
                Foreground = Brushes.Pink,
                Margin = new(10)
            };
            ToggleSwitch CommandSwitch = new()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                IsChecked = this.Enabled,
                Margin = new(10)
            };
            CommandSwitch.Click += CommandEnabledChanged;
            CommandInfo.Children.Add(CommandName);
            CommandInfo.Children.Add(CommandSwitch);
            MainStack.Children.Add(CommandInfo);
            MainStack.Children.Add(Spacer);
            
            if (false)
            {
                DockPanel ValidCommands = new();
                Card ValidCommandsContainer = new() { Margin = S2tackMargin };
                StackPanel ValidCommandsPanel = new();
                if (this.ValidCommands is not null && this.ValidCommands.Count > 0)
                {
                    foreach (var ksp in this.ValidCommands)
                    {
                        TextBox tb = new()
                        {
                            Text = ksp,
                            Foreground = Brushes.Yellow,
                            Margin = new(-10, -10, -10, 20),
                            FontSize = 14
                        };
                        tb.TextChanged += ValidCommandsChanged;
                        ValidCommandsPanel.Children.Add(tb);
                    }
                    Wpf.Ui.Controls.Button AddCommand = new()
                    {
                        Content = "+",
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new(-10),
                        FontSize = 14
                    };
                    AddCommand.Click += AddCommandClick;
                    ValidCommandsPanel.Children.Add(AddCommand);
                }
                else
                {
                    DockPanel x = new() { Margin = new(0) };
                    TextBlock h = new()
                    {
                        Text = "ANY",
                        Foreground = Brushes.LightYellow,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new(0),
                        FontSize = 14
                    };
                    x.Children.Add(h);
                    Wpf.Ui.Controls.Button v = new()
                    {
                        HorizontalAlignment = HorizontalAlignment.Right,
                        FontSize = 10,
                        Background = Brushes.Transparent,
                        Foreground = Brushes.Red,
                        Margin = new(0),
                        Width = 30,
                        Height = 30,
                        Content = "X"
                    };
                    v.Click += RemoveAnyClick;
                    x.Children.Add(v);
                    ValidCommandsPanel.Children.Add(x);
                }
                ValidCommandsContainer.Content = ValidCommandsPanel;
                ValidCommands.Children.Add(ValidCommandsContainer);
                MainStack.Children.Add(ValidCommands);
            }
            DockPanel MemberMode = new();
            ToggleSwitch ToggleMemeberMode = new()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                IsChecked = this.MemberOrPay.MemberOnly,
                Margin = new(10)
            };
            ToggleMemeberMode.Click += MemeberModeChanged;
            MemberMode.Children.Add(new TextBlock()
            {
                Text = "Member Only",
                Foreground = Brushes.LightGreen,
                Padding = new(10)
            });
            MemberMode.Children.Add(ToggleMemeberMode);
            MainStack.Children.Add(MemberMode);
            DockPanel Price = new() { Margin = new(0, 10, 0, 0) };
            NumberBox NumberInput = new()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Min = 0,
                ClearButtonEnabled = false,
                IsReadOnly = false,
                IntegersOnly = false,
                DecimalPlaces = 2,
                Step = 1,
                MaxWidth = 180,
                MinWidth = 180,
                TextAlignment = TextAlignment.Right,
                Value = this.MemberOrPay.Price,
                Padding = new(10)
            };
            NumberInput.TextChanged += PriceChanged;
            NumberInput.PreviewKeyDown += PriceKeyDown;
            Price.Children.Add(new TextBlock()
            {
                Text = "Price",
                Foreground = Brushes.LawnGreen,
                Padding = new(10)
            });
            Price.Children.Add(NumberInput);
            MainStack.Children.Add(Price);
            DockPanel Timeout = new() { Margin = new(0,10,0,0)};
            NumberBox TimeoutNumber = new()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Min = 0,
                ClearButtonEnabled = false,
                IsReadOnly = false,
                IntegersOnly = true,
                DecimalPlaces = 0,
                Step = 1,
                MaxWidth = 180,
                MinWidth = 180,
                TextAlignment = TextAlignment.Right,
                Value = this.Timeout,
                Padding = new(10)
            };
            TimeoutNumber.TextChanged += TimeoutChanged;
            TimeoutNumber.PreviewKeyDown += TimeoutKeyDown;
            Timeout.Children.Add(new TextBlock()
            {
                Text = "Timeout",
                Foreground = Brushes.LightBlue,
                Padding = new(10)
            });
            Timeout.Children.Add(TimeoutNumber);
            MainStack.Children.Add(Timeout);
            if (this.Arguments is not null && false)
            {
                StackPanel CardContent = new() { Margin = new(-15) };
                foreach (ChatCommand arg in this.Arguments)
                {
                    CardContent.Children.Add(arg.ToCard(this.SelfLevel+1));
                }
                Grid g = new()
                {
                    Margin = ArgMargin
                };
                CardExpander cardExpander = new()
                {
                    Header = "Arguments",
                    Foreground = Brushes.Orange,
                    Margin = new(0),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new(0),
                    Content = CardContent
                };
                Border b = new()
                {
                    BorderThickness = new(1),
                    BorderBrush = Brushes.DarkGray,
                    VerticalAlignment = VerticalAlignment.Top,
                    Height = 50,
                    CornerRadius = new(3)
                };
                g.Children.Add(b);
                g.Children.Add(cardExpander);
                MainStack.Children.Add(g);
            }
            this.Self.Width = BaseWidth;
            this.Self.Margin = BaseMargin;
            this.Self.VerticalAlignment = VerticalAlignment.Top;
            this.Self.Content = MainStack;
            this.InitSelf = true;
            return this.Self;
        }
        private void ValidCommandsChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            StackPanel parent = tb.Parent as StackPanel;
            if (tb.Text.Equals("ANY"))
            {
                parent.Children.Remove(tb);
                DockPanel x = new() { Margin = new(0) };
                TextBlock h = new()
                {
                    Text = "ANY",
                    Foreground = Brushes.LightYellow,
                    VerticalAlignment= VerticalAlignment.Center,
                    Margin = new(0),
                    FontSize = 14
                };
                x.Children.Add(h);
                Wpf.Ui.Controls.Button v = new Wpf.Ui.Controls.Button()
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    FontSize = 10,
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Red,
                    Margin = new(0),
                    Width = 30,
                    Height = 30,
                    Content = "X"
                };
                v.Click += RemoveAnyClick;
                x.Children.Add(v);
                parent.Children.Clear();
                parent.Children.Add(x);
            }
            else if (tb.Text.Length == 0)
            {
                int ind = parent.Children.IndexOf(tb);
                if (parent.Children.Count - 1 > this.ValidCommands.Count)
                    ind--;
                this.ValidCommands.RemoveAt(ind);
                parent.Children.Remove(tb);
            }
            else
            {
                int ind = parent.Children.IndexOf(tb);
                if(parent.Children.Count - 1 > this.ValidCommands.Count)
                    ind--;
                this.ValidCommands[ind] = tb.Text;
            }
        }
        private void RemoveAnyClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as Wpf.Ui.Controls.Button).Parent as DockPanel;
            var parent = item.Parent as StackPanel;
            parent.Children.Remove(item);
            Wpf.Ui.Controls.Button AddCommand = new()
            {
                Content = "+",
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new(-10),
                FontSize = 14
            };
            AddCommand.Click += AddCommandClick;
            parent.Children.Add(AddCommand);
        }
        private void AddCommandClick(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Controls.Button bb = sender as Wpf.Ui.Controls.Button;
            StackPanel parent = bb.Parent as StackPanel;
            TextBox tb = new()
            {
                Text = string.Empty,
                PlaceholderEnabled = true,
                PlaceholderText = "empty",
                Foreground = Brushes.Yellow,
                Margin = new(-10, -10, -10, 20),
                FontSize = 14
            };
            tb.TextChanged += ValidCommandsChanged;
            parent.Children.Insert(parent.Children.Count - 1, tb);
            this.ValidCommands.Add(string.Empty);
        }
        private void PriceKeyDown(object sender, KeyEventArgs e)
        {
            var nb = sender as NumberBox;
            if (NumberKeys.ContainsKey(e.Key))
            {
                string key = NumberKeys[e.Key].ToString();
                var decimalRegex = new Regex(DecimalRegex);
                if (decimalRegex.IsMatch(key))
                {
                    int start = nb.CaretIndex;
                    int v = Math.Max(nb.CaretIndex - 1, 0);
                    int pI = nb.Text.IndexOf('.');
                    if (pI >= 0)
                    {
                        if (pI < v)
                        {
                            int i = nb.Text.IndexOf('.');
                            int lenDiff = nb.Text.Length;
                            nb.Text = nb.Text[..i] + nb.Text[i+1] + '.' + nb.Text[(i + 2)..];
                            v += (nb.Text.Length - lenDiff);
                        }
                        else
                        {
                            v++;
                        }
                    }
                    if (start == 0)
                        v -= 1;
                    int Ci = nb.CaretIndex;
                    nb.Text = nb.Text[..Math.Max(v, 0)] + key + nb.Text[Math.Max(v, 0)..];
                    if (pI == Ci && nb.Text.Length == 4)
                        v--;
                    nb.CaretIndex = Math.Max(v + 1, 0);
                    e.Handled = true;
                }
            }
            else if (IndexResetKeys.Contains(e.Key))
            {
                if (nb.SelectedText.Length > 0)
                {
                    int c = nb.CaretIndex;
                    nb.Text = nb.Text[..nb.SelectionStart] + nb.Text[(nb.SelectionStart + nb.SelectedText.Length)..];
                    nb.CaretIndex = c;
                }
                else
                {
                    int demo = e.Key == Key.Delete ? 0 : -1;
                    int ind = Math.Max(nb.CaretIndex + demo * 2, 0);
                    int i = Math.Max(nb.CaretIndex + demo, 0);
                    if (i < nb.Text.Length)
                        if (nb.Text[i].Equals('.') || nb.Text[i].Equals(','))
                            i++;
                    int k = Math.Min(i + 1, nb.Text.Length);
                    nb.Text = nb.Text[..i] + nb.Text[k..];
                    nb.CaretIndex = ind - demo;
                }
                e.Handled = true;
            }
        }
        private void CommandEnabledChanged(object sender, RoutedEventArgs e)
        {
            this.Enabled = (sender as ToggleSwitch)?.IsChecked == true;
            if (callback is not null && this.InitSelf) Task.Run(this.callback.Invoke);
        }
        private void MemeberModeChanged(object sender, RoutedEventArgs e)
        {
            this.MemberOrPay.MemberOnly = (sender as ToggleSwitch)?.IsChecked == true;
            if (callback is not null && this.InitSelf) Task.Run(this.callback.Invoke);
        }
        private void PriceChanged(object sender, TextChangedEventArgs e)
        {
            this.MemberOrPay.Price = (sender as NumberBox)?.Value ?? 0;
            if(callback is not null && this.InitSelf) Task.Run(this.callback.Invoke);
        }
        public void RegisterOnPropertyChangedCallback(Action callback)
        {
            this.callback = callback;
        }
        private void TimeoutKeyDown(object sender, KeyEventArgs e)
        {
            var nb = sender as NumberBox;
            if (NumberKeys.ContainsKey(e.Key))
            {
                string key = NumberKeys[e.Key].ToString();
                var decimalRegex = new Regex(IntRegex);
                if (decimalRegex.IsMatch(key))
                {
                    int v = nb.CaretIndex;
                    nb.Text = nb.Text[..Math.Max(v, 0)] + key + nb.Text[Math.Max(v, 0)..];
                    nb.CaretIndex = Math.Max(v + 1, 0);
                    e.Handled = true;
                }
            }
            else if (IndexResetKeys.Contains(e.Key))
            {
                if (nb.SelectedText.Length > 0)
                {
                    int c = nb.CaretIndex;
                    nb.Text = nb.Text[..nb.SelectionStart] + nb.Text[(nb.SelectionStart + nb.SelectedText.Length)..];
                    nb.CaretIndex = c;
                }
                else
                {
                    int demo = e.Key == Key.Delete ? 0 : -1;
                    int ind = Math.Max(nb.CaretIndex + demo * 2, 0);
                    int i = Math.Max(nb.CaretIndex + demo, 0);
                    if (i < nb.Text.Length)
                        if (nb.Text[i].Equals('.') || nb.Text[i].Equals(','))
                            i++;
                    int k = Math.Min(i + 1, nb.Text.Length);
                    nb.Text = nb.Text[..i] + nb.Text[k..];
                    nb.CaretIndex = ind - demo;
                }
                e.Handled = true;
            }
        }
        private void TimeoutChanged(object sender, TextChangedEventArgs e)
        {
            this.Timeout = (int)((sender as NumberBox)?.Value ?? 0);
            if (callback is not null && this.InitSelf) Task.Run(this.callback.Invoke);
        }
    }
    public class ChatCommands
    {
        [JsonProperty("prefix")]
        public List<string> Prefix { get; set; }

        [JsonProperty("commands")]
        public List<ChatCommand> Commands { get; set; }
        public ChatCommands()
        {
            Prefix = new();
            Commands = new();
        }
    } 
    public enum CommandType
    {
        Default,
        Optional,
        Base
    }
}