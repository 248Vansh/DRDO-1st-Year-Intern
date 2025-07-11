This project was developed during my Summer Internship at DRDO, Jodhpur in 2025. It is a WPF-based desktop application that integrates Esri's ArcGIS Maps SDK for .NET.

Features
Renders interactive basemap using ArcGIS SDK
Geocodes user-entered addresses and marks them on the map
Supports routing between multiple locations with turn-by-turn directions
Implements OAuth2 authentication using ArcGIS Identity login

Tech Stack
C#
WPF (.NET 8)
ArcGIS Maps SDK for .NET
MVVM (basic usage)

Relevant Files
Only the following files are relevant to the core logic of the application:
App.xaml
App.xaml.cs
MainWindow.xaml
MainWindow.xaml.cs
ArcGISLoginPrompt.cs
MapViewModel.cs

Notes
API credentials (client ID, redirect URI, etc.) are configured locally and not pushed to the repository for security reasons.
