using System.Diagnostics;
using System.Windows;
using WebViewAddAllowedWebObjectWorkaround.Shared;

namespace WebViewAddAllowedWebObjectWorkaround_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WebView.DOMContentLoaded += (o, e) => Title = WebView.DocumentTitle;

            // This is a custom text writer trace listener to send diagnostic data to the webview log
            Trace.Listeners.Add(new WebViewTraceListener(WebView));

            // Ensure the following properties are set. They can be set programatically, or through the designer
            WebView.IsScriptNotifyAllowed = true;
            WebView.IsJavaScriptEnabled = true;

            // Ensure the JavaScript bridge is loaded. It can be loaded:
            //  - Explicitly by the remote resource by including it in the markup from a HTTP server, or
            //  - Explicitly through C# by handling the NavigationCompleted event.
            //
            // NOTE: If the JavaScript bridge is already loaded, it will not be loaded again.
            WebView.NavigationCompleted += WebViewEventHandlers.OnWebViewNavigationCompleted;
        }

        private void WebView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Perform the navigation as you normally would. This could either by through the Source property,
            // or through one of the navigation methods.
            //
            // Using the method to navigate to local content for our example
            WebView.NavigateToLocal("/Example.html");
        }
    }
}
