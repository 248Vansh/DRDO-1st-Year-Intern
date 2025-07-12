using System;
using System.Windows;
using UserAuth;

namespace DRDO
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set up OAuth challenge handler
                ArcGISLoginPrompt.SetChallengeHandler();

                // Attempt login
                bool success = await ArcGISLoginPrompt.EnsureAGOLCredentialAsync();
                if (success)
                {
                    // ✅ Open MainWindow only after successful login
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();

                    // ✅ Run post-login initialization (now that token exists)
                    await mainWindow.PostLoginInitializeAsync();

                    // ✅ Close login window
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Login failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login error: {ex.Message}");
            }
        }

        private void CloseApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


    }
}

