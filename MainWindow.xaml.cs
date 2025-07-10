using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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

