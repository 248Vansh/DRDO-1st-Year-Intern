using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Mapping;


namespace DRDO
{
    public partial class MainWindow : Window
    {
        private GraphicsOverlay _overlay;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure the overlay collection exists
            if (MyMapView.GraphicsOverlays == null)
            {
                MyMapView.GraphicsOverlays = new GraphicsOverlayCollection();
            }

            // Get the first overlay or create one if not available
            _overlay = MyMapView.GraphicsOverlays.FirstOrDefault();

            if (_overlay == null)
            {
                _overlay = new GraphicsOverlay();
                MyMapView.GraphicsOverlays.Add(_overlay);
            }
        }

        private bool _isUsingStreets = true;

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
                // Switch to 2D map view
                MySceneView.Visibility = Visibility.Collapsed;
                MyMapView.Visibility = Visibility.Visible;
                SceneToggleButton.Content = "Show 3D Scene";
            }
            else
            {
                // Switch to 3D scene view
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






        private void AddPoint_Click(object sender, RoutedEventArgs e)
        {
            StopDrawing(); // Stop any current editing
            MyMapView.GeometryEditor.Start(GeometryType.Point);
        }

        private void AddLine_Click(object sender, RoutedEventArgs e)
        {
            StopDrawing(); // Stop any current editing
            MyMapView.GeometryEditor.Start(GeometryType.Polyline);
        }

        private void AddPolygon_Click(object sender, RoutedEventArgs e)
        {
            StopDrawing(); // Stop any current editing
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
                MyMapView.GeometryEditor.Stop(); // Cancel and discard
            }
        }

        private void FinishDrawing_Click(object sender, RoutedEventArgs e)
        {
            if (!MyMapView.GeometryEditor.IsStarted) return;

            Geometry geometry = MyMapView.GeometryEditor.Stop(); // Get final shape
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

