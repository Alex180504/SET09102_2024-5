using CommunityToolkit.Maui.Views;

namespace SET09102_2024_5.Controls
{
    public partial class DatabaseErrorPopup : Popup
    {
        public DatabaseErrorPopup(string errorDetails)
        {
            InitializeComponent();
        }

        private void ExitButton_Clicked(object sender, EventArgs e)
        {
            Close();
            Application.Current.Quit(); 
        }
    }
}
