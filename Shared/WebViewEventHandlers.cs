using System.Diagnostics;
using JavaScriptBridge;
using Microsoft.Toolkit.Win32.UI.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;

namespace WebViewAddAllowedWebObjectWorkaround.Shared
{
    public static class WebViewEventHandlers
    {
        // Handle the WebView's NavigationCompleted event, ensuring the JavaScript bridge is loaded
        // This method also adds a method to subscribe to the ScriptNotify event, which will be triggered
        // by the JavaScript bridge
        public static void OnWebViewNavigationCompleted(object sender, WebViewControlNavigationCompletedEventArgs e)
        {
            // White list
            // We only load the JavaScript bridge into origins we trust
            if (e.Uri == Constants.PermittedOrigin)
            {
                if (sender is IWebView webView)
                {
                    webView.LoadBridge(e);
                    webView.ScriptNotify += OnWebViewScriptNotify;
                }
            }
            else
            {
                Trace.WriteLine($"Navigation to unknown origin {e.Uri.Host}");
            }
        }

        // Handles the ScriptNotify event for the WebView. Since there may be other uses of
        // window.external.notify at play, this method checks to see if the message came from
        // the bridge before processing it. If it did, trigger processing of all queued
        // messages within the bridge.
        private static void OnWebViewScriptNotify(object sender, WebViewControlScriptNotifyEventArgs e)
        {
            Trace.WriteLine($"Entering {typeof(WebViewEventHandlers).FullName}.{nameof(OnWebViewScriptNotify)}");

            // White list
            // WinForms/WPF do not validate the origins for ScriptNotify like UWP does
            // This needs to be done manually for security
            if (e.Uri == Constants.PermittedOrigin)
            {
                var message = e.Value;

                if (sender is IWebView webView && webView.IsJavaScriptBridgeMessage(message))
                {
                    Trace.WriteLine($"Received command {message}");
                    // Asynchronously flush the messages from the JavaScript bridge using the
                    // method called "MessageHandler"
                    webView.FlushAsync(WebViewMessageHandler.MessageHandler);
                }
            }
            else
            {
                Trace.WriteLine($"{nameof(IWebView.ScriptNotify)} received from unknown origin {e.Uri.Host}");
            }
        }
    }
}