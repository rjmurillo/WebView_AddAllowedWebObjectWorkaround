using System.Diagnostics;
using System.Windows.Forms;
using WebViewAddAllowedWebObjectWorkaround.Shared;

namespace WebViewAddAllowedWebObjectWorkaround
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            webView1.DOMContentLoaded += (o, e) => Text = webView1.DocumentTitle;

            // This is a custom text writer trace listener to send diagnostic data to the webview log
            Trace.Listeners.Add(new WebViewTraceListener(webView1));

            // Ensure the following properties are set. They can be set programatically, or through the designer
            webView1.IsScriptNotifyAllowed = true;
            webView1.IsJavaScriptEnabled = true;

            // Ensure the JavaScript bridge is loaded. It can be loaded:
            //  - Explicitly by the remote resource by including it in the markup from a HTTP server, or
            //  - Explicitly through C# by handling the NavigationCompleted event.
            //
            // NOTE: If the JavaScript bridge is already loaded, it will not be loaded again.
            webView1.NavigationCompleted += WebViewEventHandlers.OnWebViewNavigationCompleted;

            // Perform the navigation as you normally would. This could either by through the Source property,
            // or through one of the navigation methods.
            //
            // Using the method to navigate to local content for our example
            webView1.NavigateToLocal("/Example.html");
        }
    }
}
