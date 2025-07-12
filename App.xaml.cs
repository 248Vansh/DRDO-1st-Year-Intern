using System;
using System.Windows;
using Esri.ArcGISRuntime;

namespace DRDO
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize ArcGIS SDK environment
                ArcGISRuntimeEnvironment.Initialize();

                // Optional: Enable timestamp offset support
                ArcGISRuntimeEnvironment.EnableTimestampOffsetSupport = true;

                // Show login window as startup
                new LoginWindow().Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ArcGIS Runtime initialization failed:\n{ex.Message}");
                Shutdown();
            }
        }
    }
}



