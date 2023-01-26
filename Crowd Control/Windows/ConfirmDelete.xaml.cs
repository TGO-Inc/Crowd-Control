using System.Collections.Generic;
using Wpf.Ui.Controls;

namespace CrowdControl
{
    /// <summary>
    /// Interaction logic for CreateNewProfile.xaml
    /// </summary>
    public partial class ConfirmDelete : UiWindow
    {
        private bool Delete = false;
        public ConfirmDelete()
        {
            Wpf.Ui.Appearance.Theme.ApplyDarkThemeToWindow(this);
            InitializeComponent();
            Wpf.Ui.Appearance.Background.Apply(this, Wpf.Ui.Appearance.BackgroundType.Tabbed);
        }
        public bool ConfirmDeletion()
        {
            this.ShowDialog();
            return this.Delete;
        }
        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Delete = true;
            this.Close();
        }
        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
