using Microsoft.Toolkit.Win32.UI.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace JavaScriptBridge
{
    /// <summary>
    /// Contains extension methods for <see cref="IWebView"/>
    /// </summary>
    public static class WebViewExtensions
    {
        /// <summary>
        /// Dispatch a <see cref="JavaScriptBridgeMessage"/>.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView"/> instance.</param>
        /// <param name="javaScriptBridgeMessage">The message.</param>
        public static void DispatchMessage(this IWebView webView, JavaScriptBridgeMessage javaScriptBridgeMessage)
        {
            var json = javaScriptBridgeMessage.Serialize();

            if (!string.IsNullOrEmpty(json))
            {
                Trace.WriteLine("Calling JavaScriptBridge.handleNativeMessage", Constants.TraceName);
                Trace.WriteLine(javaScriptBridgeMessage, Constants.TraceName);
                var command = $"JavaScriptBridge.handleNativeMessage('{json}');";
                // in WPF ensure UI access
                webView.TryEvalAsync(command);
            }
        }

        /// <summary>
        /// Performs asynchronous JavaScript eval without waiting for return.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView"/> instance.</param>
        /// <param name="script">The script to evaluate.</param>
        public static void Eval(this IWebView webView, string script)
        {
            webView.EvalAsync(script);
        }

        /// <summary>
        /// Performs asynchronous JavaScript eval using <see cref="IWebView.InvokeScriptAsync(string, string[])" />.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView"/> instance.</param>
        /// <param name="script">The script to evaluate.</param>
        /// <returns>A <see cref="Task{String}"/>.</returns>
        public static Task<string> EvalAsync(this IWebView webView, string script)
        {
            return webView?.InvokeScriptAsync("eval", script);
        }

        /// <summary>
        /// Retrieve messages from JavaScript bridge and process them using <paramref name="messageHandler" />.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <returns></returns>
        public static async Task FlushAsync(this IWebView webView, Action<JavaScriptBridgeMessage> messageHandler)
        {
            if (webView != null)
            {
                Trace.WriteLine("Calling JavaScript method JavaScriptBridge.fetchQueue()", Constants.TraceName);

                var messageQueue = await webView.EvalAsync("JavaScriptBridge.fetchQueue()");
                if (!string.IsNullOrEmpty(messageQueue))
                {
                    var messages = JsonConvert.DeserializeObject<List<JavaScriptBridgeMessage>>(messageQueue);
                    if (messages != null)
                    {
                        Trace.WriteLine($"Retrieved {messages.Count} message{(messages.Count != 1 ? "s" : string.Empty)}", Constants.TraceName);

                        foreach (var message in messages)
                        {
                            try
                            {
                                messageHandler(message);

                                // Null the data out to avoid dealing with serialization issues in DispatchMessage
                                message.HandlerData = null;

                                // Assume that if we got this far that the call was successful
                                // If there is a callback, invoke that
                                if (!string.IsNullOrEmpty(message.CallbackId))
                                {
                                    webView.DispatchMessage(message);
                                }
                            }
                            catch (Exception exception)
                            {
                                Trace.WriteLine($"Error while processing message: [{exception.GetType()}] {exception.Message}", Constants.TraceName);

                                // Encountered an excpetion while processing the message
                                // Mask any potential disclosure issues by wrapping the exception as our own
                                var e = new JavaScriptBridgeException(exception.Message);
                                e.Source = Constants.TraceName;
                                message.ErrorData = e;

                                // Null the data out to avoid dealing with serialization issues in DispatchMessage
                                message.HandlerData = null;


                                if (!string.IsNullOrEmpty(message.CallbackId))
                                {
                                    webView.DispatchMessage(message);
                                }
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

        /// <summary>
        /// Determines if the <paramref name="message"/> is from the JavaScript bridge.
        /// </summary>
        /// <param name="_">A <see cref="IWebView"/> instance.</param>
        /// <param name="message">The contents of <see cref="WebViewControlScriptNotifyEventArgs.Value"/>.</param>
        /// <returns><see langword="true" /> if message originated from JavaScript bridge; otherwise, <see langword="false" />.</returns>
        public static bool IsJavaScriptBridgeMessage(this IWebView _, string message)
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

        /// <summary>
        /// Loads the JavaScript bridge into a <paramref name="webView"/> instance if not already loaded.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView"/> instance.</param>
        /// <param name="args">An instance of <see cref="WebViewControlNavigationCompletedEventArgs"/>.</param>
        /// <remarks>
        /// Bridge is not loaded unless the navigation was successful.
        /// </remarks>
        public static async void LoadBridge(this IWebView webView, WebViewControlNavigationCompletedEventArgs args)
        {
            if (args.IsSuccess && webView != null)
            {
                Trace.WriteLine("Executing JavaScript to detect presence of JavaScriptBridge", Constants.TraceName);

                var result = await webView.InvokeScriptAsync("eval", "typeof JavaScriptBridge == 'object'");
                if (!"true".Equals(result))
                {
                    // NOT already loaded. Load from disk
                    Trace.WriteLine("Could not detect JavaScriptBridge. Injecting script", Constants.TraceName);

                    var contents = File.ReadAllText(Constants.BridgeJavaScript);
                    webView.Eval(contents);
                }
            }
        }

        /// <summary>
        /// Attempts to asynchronously evaluate the <paramref name="script"/> in the <paramref name="webView"/> without waiting for return.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView"/> instance.</param>
        /// <param name="script">The script to evaluate.</param>
        public static void TryEvalAsync(this IWebView webView, string script)
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
    }
}
