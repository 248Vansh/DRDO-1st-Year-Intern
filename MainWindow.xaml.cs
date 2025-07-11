using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;

namespace DRDO
{
    public partial class MainWindow : Window
    {
        private GraphicsOverlay _overlay;
        private bool _isUsingStreets = true;

        private MapPoint _startPoint;
        private MapPoint _endPoint;

        private readonly Dictionary<string, LocationDisplayAutoPanMode> _autoPanModes =
            new()
            {
                { "AutoPan Off", LocationDisplayAutoPanMode.Off },
                { "Re-Center", LocationDisplayAutoPanMode.Recenter },
                { "Navigation", LocationDisplayAutoPanMode.Navigation },
                { "Compass", LocationDisplayAutoPanMode.CompassNavigation }
            };

        private readonly Uri _geocodeServiceUri = new("https://geocode-api.arcgis.com/arcgis/rest/services/World/GeocodeServer");
        private LocatorTask _geocoder;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            // Setup location display autopan options
            AutoPanModeComboBox.ItemsSource = _autoPanModes.Keys;
            AutoPanModeComboBox.SelectedItem = "AutoPan Off";

            MyMapView.LocationDisplay.AutoPanModeChanged += (s, e) =>
            {
                if (MyMapView.LocationDisplay.AutoPanMode == LocationDisplayAutoPanMode.Off)
                    AutoPanModeComboBox.SelectedItem = "AutoPan Off";
            };

            Unloaded += (s, e) => MyMapView.LocationDisplay?.DataSource?.StopAsync();

            // Hook internal TextBox's TextChanged event
            SearchBox.Loaded += (s, e) =>
            {
                if (SearchBox.Template.FindName("PART_EditableTextBox", SearchBox) is TextBox innerTextBox)
                {
                    innerTextBox.TextChanged += SearchBox_TextChanged;
                }
            };

            // Initialize LocatorTask
            _ = InitializeGeocoder();
        }




        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure overlay
            if (MyMapView.GraphicsOverlays == null)
                MyMapView.GraphicsOverlays = new GraphicsOverlayCollection();

            _overlay = MyMapView.GraphicsOverlays.FirstOrDefault() ?? new GraphicsOverlay();
            if (!MyMapView.GraphicsOverlays.Contains(_overlay))
                MyMapView.GraphicsOverlays.Add(_overlay);

            // Hook map tap for reverse geocoding
            MyMapView.GeoViewTapped += MyMapView_GeoViewTapped;
        }

        private readonly Uri _routeServiceUri = new Uri("https://route.arcgis.com/arcgis/rest/services/World/Route/NAServer/Route_World");

        // Find Route Button Click Handler
        private async void SolveRoute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_startPoint == null || _endPoint == null)
                {
                    MessageBox.Show("Please search and select both start and end addresses.");
                    return;
                }

                var stops = new List<Stop>
        {
            new Stop(_startPoint),
            new Stop(_endPoint)
        };

                RouteTask routeTask = await RouteTask.CreateAsync(_routeServiceUri);
                RouteParameters routeParams = await routeTask.CreateDefaultParametersAsync();

                routeParams.ReturnStops = true;
                routeParams.ReturnDirections = true;
                routeParams.SetStops(stops);

                RouteResult result = await routeTask.SolveRouteAsync(routeParams);
                Route route = result.Routes.FirstOrDefault();
                if (route == null)
                {
                    MessageBox.Show("No route found.");
                    return;
                }

                // Remove existing polylines
                for (int i = _overlay.Graphics.Count - 1; i >= 0; i--)
                {
                    if (_overlay.Graphics[i].Geometry is Polyline)
                        _overlay.Graphics.RemoveAt(i);
                }

                var routeGraphic = new Graphic(route.RouteGeometry,
                    new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Purple, 5));
                _overlay.Graphics.Add(routeGraphic);

                await MyMapView.SetViewpointGeometryAsync(route.RouteGeometry, 50);
                DirectionsListBox.ItemsSource = route.DirectionManeuvers.Select(d => d.DirectionText);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Route failed: " + ex.Message);
            }
        }


        private async Task InitializeGeocoder()
        {
            try
            {
                _geocoder = await LocatorTask.CreateAsync(_geocodeServiceUri);

                // ✅ Enable UI elements after successful geocoder load
                SearchBox.IsEnabled = true;
                SearchButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Geocoder initialization failed: {ex.Message}");
            }
        }

        private async Task PerformSearch(string query)
        {
            try
            {
                var parameters = new GeocodeParameters();
                var results = await _geocoder.GeocodeAsync(query, parameters);
                if (results.Count < 1) return;

                var location = results.First().DisplayLocation;

                // Decide whether it's a start or end
                if (_startPoint == null)
                {
                    _startPoint = location;
                    AddMarker(location, Color.Green, "Start");
                }
                else if (_endPoint == null)
                {
                    _endPoint = location;
                    AddMarker(location, Color.Red, "End");
                }
                else
                {
                    MessageBox.Show("Start and End already set. Click 'Stop And Clear Drawing' to reset.");
                    return;
                }

                await MyMapView.SetViewpointCenterAsync(location, 100000);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Geocode failed: " + ex.Message);
            }
        }


        private void AddMarker(MapPoint point, Color color, string label)
        {
            var symbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, color, 15);
            var graphic = new Graphic(point, symbol);
            graphic.Attributes["Label"] = label;
            _overlay.Graphics.Add(graphic);
        }



        private async void SearchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchBox.SelectedItem is string selected)
            {
                SearchBox.Text = selected;
                await PerformSearch(selected);
            }
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!SearchBox.IsKeyboardFocusWithin) return; // Prevent dropdown flicker during auto-select
            await UpdateSuggestionsAsync(SearchBox.Text);
        }


        private async void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            await UpdateSuggestionsAsync(SearchBox.Text);
        }

        private async Task UpdateSuggestionsAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || _geocoder == null)
                return;

            try
            {
                var suggestions = await _geocoder.SuggestAsync(input);
                if (suggestions.Count == 0) return;

                SearchBox.ItemsSource = suggestions.Select(s => s.Label).ToList();
                SearchBox.IsDropDownOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Suggestion failed: " + ex.Message);
            }
        }






        // Drawing Controls
        private void AddPoint_Click(object sender, RoutedEventArgs e) => StartGeometryEditor(GeometryType.Point);
        private void AddLine_Click(object sender, RoutedEventArgs e) => StartGeometryEditor(GeometryType.Polyline);
        private void AddPolygon_Click(object sender, RoutedEventArgs e) => StartGeometryEditor(GeometryType.Polygon);

        private void StartGeometryEditor(GeometryType type)
        {
            StopDrawing();
            MyMapView.GeometryEditor.Start(type);
        }

        private void StopDrawing_Click(object sender, RoutedEventArgs e) => StopDrawing();

        private void StopDrawing()
        {
            MyMapView.GeometryEditor.Stop();
            _overlay.Graphics.Clear();
            _startPoint = null;
            _endPoint = null;
            DirectionsListBox.ItemsSource = null;
        }


        private void FinishDrawing_Click(object sender, RoutedEventArgs e)
        {
            if (!MyMapView.GeometryEditor.IsStarted) return;

            Geometry geometry = MyMapView.GeometryEditor.Stop();
            if (geometry == null) return;

            Symbol symbol = geometry.GeometryType switch
            {
                GeometryType.Point => new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Red, 10),
                GeometryType.Polyline => new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Blue, 3),
                GeometryType.Polygon => new SimpleFillSymbol(
                    SimpleFillSymbolStyle.Solid,
                    Color.FromArgb(120, 255, 165, 0),
                    new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Black, 2)
                ),
                _ => null
            };

            if (symbol != null)
            {
                Graphic graphic = new(geometry, symbol);
                _overlay.Graphics.Add(graphic);
            }
        }

        private void ToggleBasemap_Click(object sender, RoutedEventArgs e)
        {
            MyMapView.Map.Basemap = _isUsingStreets
                ? new Basemap(BasemapStyle.ArcGISImagery)
                : new Basemap(BasemapStyle.ArcGISStreets);

            ToggleBasemapButton.Content = _isUsingStreets ? "Switch to Streets" : "Switch to Imagery";
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
                        MySceneView.Scene = new Scene(BasemapStyle.ArcGISImageryStandard);
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
                MyMapView.LocationDisplay.AutoPanMode =
                    _autoPanModes[AutoPanModeComboBox.SelectedItem.ToString()];
        }

        private async void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MyMapView.LocationDisplay.IsEnabled)
                    await MyMapView.LocationDisplay.DataSource.StopAsync();
                else
                    await MyMapView.LocationDisplay.DataSource.StartAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Location Error: " + ex.Message);
            }

            StartStopButton.Content = MyMapView.LocationDisplay.IsEnabled ? "Stop" : "Start";
        }

        // --- Geocoding Features ---
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchBox.Text;
            if (string.IsNullOrWhiteSpace(query) || _geocoder == null)
                return;

            await PerformSearch(query);
        }
        


        // --- Optional Step 7: Reverse Geocoding on Tap ---
        private async void MyMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                var graphicsResults = await MyMapView.IdentifyGraphicsOverlaysAsync(e.Position, 10, false);
                if (graphicsResults.Count < 1) return;

                var results = await _geocoder.ReverseGeocodeAsync(e.Location);
                if (results.Count < 1) return;

                var address = results.First();
                string title = address.Attributes.ContainsKey("City") ? address.Attributes["City"].ToString() : "Location";
                string details = address.Label;

                CalloutDefinition callout = new(title, details);
                MyMapView.ShowCalloutAt(e.Location, callout);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reverse Geocode failed: " + ex.Message);
            }
        }
    }
}



