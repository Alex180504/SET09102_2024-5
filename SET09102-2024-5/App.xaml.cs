using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using SET09102_2024_5.Controls;

namespace SET09102_2024_5
{
    public partial class App : Application
    {
        private readonly IDatabaseInitializationService _dbInitService;

        public App(IDatabaseInitializationService dbInitService)
        {
            InitializeComponent();
            _dbInitService = dbInitService;

            MainPage = new AppShell();

            // Check database connection
            MainThread.BeginInvokeOnMainThread(CheckDatabaseConnection);
        }

        private async void CheckDatabaseConnection()
        {
            try
            {
                // Try to connect to the database
                bool isConnected = await _dbInitService.InitializeDatabaseAsync();

                if (!isConnected)
                {
                    string errorDetails = _dbInitService.GetLastErrorMessage();
                    if (string.IsNullOrEmpty(errorDetails))
                    {
                        errorDetails = "Cannot connect to the database";
                    }

                    ShowDatabaseErrorPopup(errorDetails);
                }
            }
            catch (Exception ex)
            {
                ShowDatabaseErrorPopup("Cannot connect to the database: " + ex.Message);
            }
        }

        private void ShowDatabaseErrorPopup(string details)
        {
            var popup = new DatabaseErrorPopup(details);
            MainPage.ShowPopup(popup);
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
