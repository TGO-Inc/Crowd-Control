using System.Collections.Generic;
using System.Text.RegularExpressions;
using Wpf.Ui.Controls;

namespace CrowdControl
{
    /// <summary>
    /// Interaction logic for CreateNewProfile.xaml
    /// </summary>
    public partial class NewProfileWindow : UiWindow
    {
        private bool save = false;
        private List<string> exiting_profile_list;
        private string text = string.Empty;
        public NewProfileWindow(List<string> exiting_profile_list)
        {
            this.exiting_profile_list = exiting_profile_list;
            Wpf.Ui.Appearance.Theme.ApplyDarkThemeToWindow(this);
            InitializeComponent();
            Wpf.Ui.Appearance.Background.Apply(this, Wpf.Ui.Appearance.BackgroundType.Tabbed);
        }
        public string? CreateNewProfile()
        {
            this.ShowDialog();
            if (this.save)
                return this.text;
            else
                return null;
        }
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.text = new Regex("[^a-zA-Z0-9\\.\\-]").Replace(this.TextBox.Text, "_");
            if (this.text.Length > 0)
            {
                if (!this.exiting_profile_list.Contains(this.text))
                {
                    this.save = true;
                    this.Close();
                }
                else
                {
                    this.Error.Text = "A Profile with this name already exists";
                    this.Error.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                this.Error.Text = "Please enter a profile name";
                this.Error.Visibility = System.Windows.Visibility.Visible;
            }
        }
        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
