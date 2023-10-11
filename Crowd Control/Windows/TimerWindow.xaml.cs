using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Brushes = System.Windows.Media.Brushes;

namespace CrowdControl.Windows
{
    internal class TimeClass
    {
        public bool Enabled { get; set; } = true;
        public double CurrentTime { get; private set; }
        public double BigTime { get => this.CurrentTime / 10; }
        public void Decrease(double amount) => CurrentTime -= amount;
        public void ResetTime(double target) => CurrentTime = target * 1000;
        public TimeClass(double currentTime)
        {
            CurrentTime = currentTime * 1000;
        }
    }
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class TimerView : Window
    {
        private Dictionary<string, (TextBlock Name, TextBlock Time, ProgressRing Timer, int StartTime, TimeClass CurrentTime)> timer;
        private readonly Timer UpdateTimes = new()
        {
            Interval = 100,
            AutoReset = true,
            Enabled = true,
        };

        public TimerView(Dictionary<string, int> timerValues)
        {
            Wpf.Ui.Appearance.Theme.ApplyDarkThemeToWindow(this);
            InitializeComponent();
            //Wpf.Ui.Appearance.Background.Apply(this, Wpf.Ui.Appearance.BackgroundType.Acrylic);
            this.timer = new();
            this.CreateTimers(timerValues);
            UpdateTimes.Elapsed += UpdateTimes_Elapsed;
            UpdateTimes.Start();
        }
        public Grid CreateTimerObject(KeyValuePair<string, int> timerValue, bool enabled = true)
        {
            var ring = new ProgressRing()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Progress = 100,
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection()
                        {
                            new RotateTransform()
                            {
                                Angle = 270,
                                CenterX = 30,
                                CenterY = 30,
                            },
                            new ScaleTransform()
                            {
                                ScaleX = -0.65,
                                ScaleY = 0.65,
                                CenterX = 30,
                                CenterY = 30
                            }
                        }
                }
            };
            var time = new TextBlock()
            {
                Text = enabled ? timerValue.Value.ToString() : "X",
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = enabled ? Brushes.DarkOrange : Brushes.Red,
                FontFamily = Fonts.SystemFontFamilies.Where(f => f.FamilyNames.Where(n => n.Value.Contains("Comic Sans MS")).Any()).First(),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            var name = new TextBlock()
            {
                Text = timerValue.Key,
                FontSize = 30,
                Margin = new Thickness(70, 0, 0, 0),
                Foreground = enabled ? Brushes.DarkOrange : Brushes.Red,
                VerticalAlignment = VerticalAlignment.Center,
            };
            this.timer.Add(timerValue.Key.ToLower().Trim(), (name, time, ring, timerValue.Value, new(enabled ? timerValue.Value : -1)));
            return new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                Children =
                {
                    new Grid()
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Background = Brushes.Transparent,
                        Children =
                        {
                            ring,
                            time,
                        }
                    },
                    name
                },
                Margin = new Thickness(10, 0, 10, 10)
            };
        }
        public void CreateTimers(Dictionary<string, int> timerValues)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                this.Container.Children.Clear();
                this.timer = new();
                foreach (var timerValue in timerValues)
                {
                    this.Container.Children.Add(
                        this.CreateTimerObject(timerValue)
                    );
                }
                this.Window_SizeChanged(null, null);
            });
        }

        private void UpdateTimes_Elapsed(object? sender, ElapsedEventArgs e)
        {
            foreach (var ItemSet in this.timer)
            {
                var (Name, Time, Timer, StartTime, TimeClass) = ItemSet.Value;
                if (TimeClass.CurrentTime > 0)
                {
                    TimeClass.Decrease(this.UpdateTimes.Interval);
                    this.Dispatcher.Invoke(() =>
                    {
                        Timer.Progress = TimeClass.BigTime / StartTime;
                        Time.Text = ((int)TimeClass.CurrentTime / 1000).ToString();
                    });
                }
                else if (TimeClass.CurrentTime == 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Name.Foreground = Brushes.LimeGreen;
                        Time.Foreground = Brushes.LimeGreen;
                    });
                    TimeClass.ResetTime(-1);
                }
            }
        }

        public void DisableTimer(string name)
        {
            name = name.ToLower().Trim();
            if (this.timer.ContainsKey(name))
            {
                this.timer[name].CurrentTime.ResetTime(-1);
                this.timer[name].CurrentTime.Enabled = false;
                this.Dispatcher.Invoke(() =>
                {
                    this.timer[name].Name.Foreground = Brushes.Red;
                    this.timer[name].Time.Foreground = Brushes.Red;
                    this.timer[name].Time.Text = "X";
                    this.timer[name].Timer.Progress = 100;
                });
            }
        }

        public void EnableTimer(string name)
        {
            name = name.ToLower().Trim();
            if (this.timer.ContainsKey(name))
            {
                if (this.timer[name].CurrentTime.Enabled)
                    return;

                this.timer[name].CurrentTime.Enabled = true;
                this.timer[name].CurrentTime.ResetTime(this.timer[name].StartTime);
                this.Dispatcher.Invoke(() =>
                {
                    this.timer[name].Name.Foreground = Brushes.DarkOrange;
                    this.timer[name].Time.Foreground = Brushes.DarkOrange;
                    this.timer[name].Time.Text = "0";
                });

            }
        }

        public void ResetTimer(string name)
        {
            if (this.timer.ContainsKey(name))
            {
                this.timer[name].CurrentTime.ResetTime(this.timer[name].StartTime);
                this.Dispatcher.Invoke(() =>
                {
                    this.timer[name].Name.Foreground = Brushes.DarkOrange;
                    this.timer[name].Time.Foreground = Brushes.DarkOrange;
                });
            }

        }

        public void ResetRandomTimer()
        {
            int rand = new Random().Next(0, this.timer.Keys.Count);
            for (int i = 0; i < this.timer.Keys.Count; i++)
            {
                if (i == rand)
                {
                    string key = this.timer.Keys.ElementAt(i);
                    this.timer[key].CurrentTime.ResetTime(this.timer[key].StartTime);
                }
            }
        }

        private void UiWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.UpdateTimes.Stop();
            this.UpdateTimes.Dispose();
            this.timer.Clear();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Topmost = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Height = this.Container.ActualHeight + 25 + 15;
            this.Width = this.Container.ActualWidth + 25;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Height = this.Container.ActualHeight + 25 + 15;
            this.Width = this.Container.ActualWidth + 25;
        }
        public void AddTimer(string key, int time, bool enabled = true)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (!this.timer.ContainsKey(key))
                {
                    this.Container.Children.Add(
                        this.CreateTimerObject(new KeyValuePair<string, int>(key, time), enabled)
                    );
                }
            });
        }
        public void ClearTimers()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Container.Children.Clear();
                this.timer = new();
            });
        }
    }
}
