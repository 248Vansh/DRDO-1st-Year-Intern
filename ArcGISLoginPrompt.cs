using Esri.ArcGISRuntime.Security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace UserAuth
{
    internal static class ArcGISLoginPrompt
    {
        private const string ArcGISOnlineUrl = "https://www.arcgis.com/sharing/rest";

        private const string AppClientId = "70xWEIGU08ikrF6c";
        private const string OAuthRedirectUrl = "http://localhost";

        public static async Task<bool> EnsureAGOLCredentialAsync()
        {
            bool loggedIn = false;

            try
            {
                CredentialRequestInfo challengeRequest = new CredentialRequestInfo
                {
                    GenerateTokenOptions = new GenerateTokenOptions
                    {
                        TokenAuthenticationType = TokenAuthenticationType.OAuthAuthorizationCode
                    },
                    ServiceUri = new Uri(ArcGISOnlineUrl)
                };

                Credential? cred = await AuthenticationManager.Current.GetCredentialAsync(challengeRequest, false);
                loggedIn = cred != null;
            }
            catch (OperationCanceledException)
            {
                // Login canceled
            }
            catch (Exception ex)
            {
                MessageBox.Show("Login failed: " + ex.Message);
            }

            return loggedIn;
        }

        public static void SetChallengeHandler()
        {
            var userConfig = new OAuthUserConfiguration(
                new Uri(ArcGISOnlineUrl), AppClientId, new Uri(OAuthRedirectUrl)
            );

            AuthenticationManager.Current.OAuthUserConfigurations.Add(userConfig);
            AuthenticationManager.Current.OAuthAuthorizeHandler = new OAuthAuthorize();
        }

        private class OAuthAuthorize : IOAuthAuthorizeHandler
        {
            private Window? _authWindow;
            private TaskCompletionSource<IDictionary<string, string>> _tcs;
            private string _callbackUrl = "";
            private string? _authorizeUrl;

            public Task<IDictionary<string, string>> AuthorizeAsync(Uri serviceUri, Uri authorizeUri, Uri callbackUri)
            {
                if (_tcs != null && !_tcs.Task.IsCompleted)
                    throw new Exception("Task in progress");

                _tcs = new TaskCompletionSource<IDictionary<string, string>>();

                _authorizeUrl = authorizeUri.AbsoluteUri;
                _callbackUrl = callbackUri.AbsoluteUri;

                Dispatcher dispatcher = Application.Current.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess())
                    AuthorizeOnUIThread(_authorizeUrl);
                else
                    dispatcher.BeginInvoke(new Action(() => AuthorizeOnUIThread(_authorizeUrl)));

                return _tcs.Task;
            }

            private void AuthorizeOnUIThread(string authorizeUri)
            {
                WebBrowser webBrowser = new WebBrowser();
                webBrowser.Navigating += WebBrowserOnNavigating;

                _authWindow = new Window
                {
                    Content = webBrowser,
                    Width = 450,
                    Height = 450,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (Application.Current != null && Application.Current.MainWindow != null)
                    _authWindow.Owner = Application.Current.MainWindow;

                _authWindow.Closed += OnWindowClosed;
                webBrowser.Navigate(authorizeUri);

                _authWindow.ShowDialog();
            }

            private void OnWindowClosed(object? sender, EventArgs e)
            {
                if (_authWindow?.Owner != null)
                    _authWindow.Owner.Focus();

                if (!_tcs.Task.IsCompleted)
                    _tcs.SetCanceled();

                _authWindow = null;
            }

            private void WebBrowserOnNavigating(object sender, NavigatingCancelEventArgs e)
            {
                const string portalApprovalMarker = "/oauth2/approval";
                WebBrowser? webBrowser = sender as WebBrowser;
                Uri uri = e.Uri;

                if (webBrowser == null || uri == null || string.IsNullOrEmpty(uri.AbsoluteUri))
                    return;

                bool isRedirected = uri.AbsoluteUri.StartsWith(_callbackUrl) ||
                                    (_callbackUrl.Contains(portalApprovalMarker) && uri.AbsoluteUri.Contains(portalApprovalMarker));

                if (isRedirected)
                {
                    e.Cancel = true;
                    IDictionary<string, string> authResponse = DecodeParameters(uri);
                    _tcs.SetResult(authResponse);
                    _authWindow?.Close();
                }
            }

            private static IDictionary<string, string> DecodeParameters(Uri uri)
            {
                string answer = "";

                if (!string.IsNullOrEmpty(uri.Fragment))
                    answer = uri.Fragment.Substring(1);
                else if (!string.IsNullOrEmpty(uri.Query))
                    answer = uri.Query.Substring(1);

                Dictionary<string, string> keyValueDictionary = new Dictionary<string, string>();
                string[] keysAndValues = answer.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string kvString in keysAndValues)
                {
                    string[] pair = kvString.Split('=');
                    string key = pair[0];
                    string value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : "";
                    keyValueDictionary[key] = value;
                }

                return keyValueDictionary;
            }
        }
    }
}

