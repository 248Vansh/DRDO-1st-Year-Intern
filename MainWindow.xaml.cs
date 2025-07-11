using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DRDO
{
    public partial class MainWindow : Window
    {
        private GraphicsOverlay _overlay;
        private bool _isUsingStreets = true;

        private readonly Dictionary<string, LocationDisplayAutoPanMode> _autoPanModes =
            new Dictionary<string, LocationDisplayAutoPanMode>
            {
                { "AutoPan Off", LocationDisplayAutoPanMode.Off },
                { "Re-Center", LocationDisplayAutoPanMode.Recenter },
                { "Navigation", LocationDisplayAutoPanMode.Navigation },
                { "Compass", LocationDisplayAutoPanMode.CompassNavigation }
            };

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            // AutoPan ComboBox setup
            AutoPanModeComboBox.ItemsSource = _autoPanModes.Keys;
            AutoPanModeComboBox.SelectedItem = "AutoPan Off";

            MyMapView.LocationDisplay.AutoPanModeChanged += (s, e) =>
            {
                if (MyMapView.LocationDisplay.AutoPanMode == LocationDisplayAutoPanMode.Off)
                {
                    AutoPanModeComboBox.SelectedItem = "AutoPan Off";
                }
            };

            // Optional but good: Stop location tracking when window is unloaded
            Unloaded += (s, e) =>
            {
                MyMapView.LocationDisplay?.DataSource?.StopAsync();
            };
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure overlay collection
            if (MyMapView.GraphicsOverlays == null)
            {
                MyMapView.GraphicsOverlays = new GraphicsOverlayCollection();
            }

            // Get or create overlay
            _overlay = MyMapView.GraphicsOverlays.FirstOrDefault() ?? new GraphicsOverlay();
            if (!MyMapView.GraphicsOverlays.Contains(_overlay))
                MyMapView.GraphicsOverlays.Add(_overlay);
        }

        private void ToggleBasemap_Click(object sender, RoutedEventArgs e)
        {
            if (_isUsingStreets)
            {
                MyMapView.Map.Basemap = new Basemap(BasemapStyle.ArcGISImagery);
                ToggleBasemapButton.Content = "Switch to Streets";
            }
            else
            {
                MyMapView.Map.Basemap = new Basemap(BasemapStyle.ArcGISStreets);
                ToggleBasemapButton.Content = "Switch to Imagery";
            }

            _isUsingStreets = !_isUsingStreets;
        }

        private async void ToggleScene_Click(object sender, RoutedEventArgs e)
        {
            if (MySceneView.Visibility == Visibility.Visible)
            {
                MySceneView.Visibility = Visibility.Collapsed;
                MyMapView.Visibility = Visibility.Visible;
                SceneToggleButton.Content = "Show 3D Scene";
            }
            else
            {
                if (MySceneView.Scene == null)
                {
                    try
                    {
                        Scene globalScene = new Scene(BasemapStyle.ArcGISImageryStandard);
                        MySceneView.Scene = globalScene;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to load 3D scene: " + ex.Message);
                        return;
                    }
                }

                MyMapView.Visibility = Visibility.Collapsed;
                MySceneView.Visibility = Visibility.Visible;
                SceneToggleButton.Content = "Show 2D Map";
            }
        }

        private void AutoPanModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AutoPanModeComboBox.SelectedItem != null)
            {
                MyMapView.LocationDisplay.AutoPanMode =
                    _autoPanModes[AutoPanModeComboBox.SelectedItem.ToString()];
            }
        }

        private async void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MyMapView.LocationDisplay.IsEnabled)
                {
                    await MyMapView.LocationDisplay.DataSource.StopAsync();
                }
                else
                {
                    await MyMapView.LocationDisplay.DataSource.StartAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                StartStopButton.Content = MyMapView.LocationDisplay.IsEnabled ? "Stop" : "Start";
            }
        }

        private void AddPoint_Click(object sender, RoutedEventArgs e)
        {
            StopDrawing();
            MyMapView.GeometryEditor.Start(GeometryType.Point);
        }

        private void AddLine_Click(object sender, RoutedEventArgs e)
        {
            StopDrawing();
            MyMapView.GeometryEditor.Start(GeometryType.Polyline);
        }

        private void AddPolygon_Click(object sender, RoutedEventArgs e)
        {
            StopDrawing();
            MyMapView.GeometryEditor.Start(GeometryType.Polygon);
        }

        private void StopDrawing_Click(object sender, RoutedEventArgs e)
        {
            StopDrawing();
        }

        private void StopDrawing()
        {
            if (MyMapView.GeometryEditor.IsStarted)
            {
                MyMapView.GeometryEditor.Stop();
            }
        }

        private void FinishDrawing_Click(object sender, RoutedEventArgs e)
        {
            if (!MyMapView.GeometryEditor.IsStarted) return;

            Geometry geometry = MyMapView.GeometryEditor.Stop();
            if (geometry == null) return;

            Symbol symbol = null;

            switch (geometry.GeometryType)
            {
                case GeometryType.Point:
                    symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10);
                    break;
                case GeometryType.Polyline:
                    symbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 3);
                    break;
                case GeometryType.Polygon:
                    var outline = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Black, 2);
                    symbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, Color.FromArgb(120, 255, 165, 0), outline);
                    break;
            }

            if (symbol != null)
            {
                var graphic = new Graphic(geometry, symbol);
                _overlay.Graphics.Add(graphic);
            }
        }
    }
}


