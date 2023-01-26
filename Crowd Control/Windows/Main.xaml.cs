using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace CrowdControl
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : UiWindow
    {
        public Main()
        {
            Wpf.Ui.Appearance.Theme.ApplyDarkThemeToWindow(this);
            InitializeComponent();
            Wpf.Ui.Appearance.Background.Apply(this, Wpf.Ui.Appearance.BackgroundType.Mica);
        }
        private void RootNavigation_OnNavigated(object sender, RoutedNavigationEventArgs e)
        {

        }
        private void NavigationButtonTheme_OnClick(object sender, RoutedEventArgs e)
        {

        }
        private void UiWindow_Closed(object sender, EventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }
        private void UiWindow_StateChanged(object sender, EventArgs e)
        {
            if(this.WindowState.Equals(WindowState.Maximized))
            {
                (this.Content as Grid).Margin = new(10);
            }
            else
            {
                (this.Content as Grid).Margin = new(0);
            }
        }

        private void RootNavigation_OnNavigated(Wpf.Ui.Controls.Interfaces.INavigation sender, RoutedNavigationEventArgs e)
        {

        }
    }
}
