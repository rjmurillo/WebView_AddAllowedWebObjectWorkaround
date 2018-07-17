using Microsoft.Toolkit.Win32.UI.Controls.WinForms;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;

namespace WebViewAddAllowedWebObjectWorkaround
{
    public static class WebViewExtensions
    {
        private static readonly JsonSerializerSettings MessageSerializationSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            StringEscapeHandling = StringEscapeHandling.EscapeHtml
        };

        public static void DispatchMessage(this WebView webView, JavaScriptBridgeMessage javaScriptBridgeMessage)
        {
            var json = JsonConvert.SerializeObject(javaScriptBridgeMessage, MessageSerializationSettings);

            if (!string.IsNullOrEmpty(json))
            {
                var command = $"JavaScriptBridge.handleNativeMessage('{json}');";
                // in WPF ensure UI access
                webView.TryInvokeScriptAsync(command);
            }
        }

        public static void Eval(this WebView webView, string script)
        {
            webView.EvalAsync(script);
        }

        public static Task<string> EvalAsync(this WebView webView, string script)
        {
            return webView?.InvokeScriptAsync("eval", script);
        }

        public static async Task FlushAsync(this WebView webView, Action<JavaScriptBridgeMessage> messageHandler)
        {
            if (webView != null)
            {
                var messageQueue = await webView.EvalAsync("JavaScriptBridge.fetchQueue()");
                if (!string.IsNullOrEmpty(messageQueue))
                {
                    var messages = JsonConvert.DeserializeObject<List<JavaScriptBridgeMessage>>(messageQueue);
                    if (messages != null)
                    {
                        foreach (var message in messages)
                        {
                            messageHandler(message);

                            // If there is a callback, invoke that
                            if (!string.IsNullOrEmpty(message.Callback))
                            {
                                DispatchMessage(webView, message);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Unexpected input received from JavaScript bridge: {messageQueue}");
                    }
                }
            }
        }

        public static void TryInvokeScriptAsync(this WebView webView, string script)
        {
            if (webView != null)
            {
                try
                {
                    webView.Eval(script);
                }
                catch (Exception e)
                {
                    // Best effort
                    Debug.WriteLine("Error executing command: " + script);
                    Debug.WriteLine(e.Message);
                }
            }
        }

        public static async void LoadBridge(this WebView webView, WebViewControlNavigationCompletedEventArgs args)
        {
            if (args.IsSuccess && webView != null)
            {
                var result = await webView.InvokeScriptAsync("eval", "typeof JavaScriptBridge == 'object'");
                if (!"true".Equals(result))
                {
                    // NOT already loaded. Load from disk
                    var contents = File.ReadAllText("Bridge.js");
                    webView.Eval(contents);
                }
            }
        }

        public static bool IsJavaScriptBridgeMessage(this WebView webView, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (message.StartsWith("jsbridge", StringComparison.OrdinalIgnoreCase))
                {
                    if (message.IndexOf("queue_message", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
