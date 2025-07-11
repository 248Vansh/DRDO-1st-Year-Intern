using System;
using System.Windows;
using Esri.ArcGISRuntime;
using UserAuth;

namespace DRDO
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Set up the OAuth challenge handler
                ArcGISLoginPrompt.SetChallengeHandler();

                // Initialize ArcGIS SDK
                ArcGISRuntimeEnvironment.Initialize();

                // Force user to sign in at startup (optional)
                bool success = await ArcGISLoginPrompt.EnsureAGOLCredentialAsync();
                if (!success)
                {
                    MessageBox.Show("User sign-in failed. Closing app.");
                    Shutdown();
                }

                // Enable timestamp offset support
                ArcGISRuntimeEnvironment.EnableTimestampOffsetSupport = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ArcGIS Maps SDK runtime initialization failed.");
                Shutdown();
            }
        }
    }
}


