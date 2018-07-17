using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Microsoft.Toolkit.Win32.UI.Controls.WinForms;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace WebViewAddAllowedWebObjectWorkaround
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Objects should be serialized to be sent back to JavaScript
        public string MethodArgs(string arg) => $"MethodArgs Result: {arg}";

        public string MethodNoArgs() => "MethodNoArgs Result";

        private void Form1_Load(object sender, EventArgs e)
        {
            webView1.IsScriptNotifyAllowed = true;
            webView1.IsJavaScriptEnabled = true;

            webView1.NavigateToLocal("/Example.html");
            webView1.NavigationCompleted += (o, a) =>
            {
                ((WebView) o).LoadBridge(a);
                webView1.ScriptNotify += OnWebViewScriptNotify;
            };
        }

        private void MessageHandler(JavaScriptBridgeMessage javaScriptBridgeMessage)
        {
            if (!string.IsNullOrEmpty(javaScriptBridgeMessage.Handler))
            {
                // Handle the message. Could use something more generic, like reflection
                if ("MethodNoArgs".Equals(javaScriptBridgeMessage.Handler, StringComparison.OrdinalIgnoreCase))
                {
                    javaScriptBridgeMessage.Response = MethodNoArgs();
                    // Null the data out to avoid dealing with serialization issues in DispatchMessage
                    javaScriptBridgeMessage.Data = null;
                }
                else if ("MethodArgs".Equals(javaScriptBridgeMessage.Handler, StringComparison.OrdinalIgnoreCase))
                {
                    var args = JsonConvert.DeserializeObject<List<object>>(javaScriptBridgeMessage.Data);
                    javaScriptBridgeMessage.Response = MethodArgs(args[0].ToString());
                    // Null the data out to avoid dealing with serialization issues in DispatchMessage
                    javaScriptBridgeMessage.Data = null;
                }
            }
        }

        private void OnWebViewScriptNotify(object sender, WebViewControlScriptNotifyEventArgs e)
        {
            var message = e.Value;

            if (sender is WebView webView && webView.IsJavaScriptBridgeMessage(message))
            {
                Debug.WriteLine($"Received command {message}");
                webView.FlushAsync(MessageHandler);
            }
        }
    }
}
