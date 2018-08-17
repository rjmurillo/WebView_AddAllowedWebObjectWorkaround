using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions
{
    /// <summary>
    /// A means of communicating bi-directionally with JavaScript on a web page.
    /// </summary>
    public class JavaScriptBridge
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptBridge"/> class.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView"/> instance.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="webView"/> is null.</exception>
        public JavaScriptBridge(IWebView webView)
        {
            WebView = webView ?? throw new ArgumentNullException(nameof(webView));

            ScriptingDelegates = new Dictionary<string, Func<Dictionary<string, object>, object>>(StringComparer.OrdinalIgnoreCase);
            AllowedScriptNotifyUris = new HashSet<Uri>();

            // Ensure the following properties are set: We rely on JavaScript and ScriptNotify for the bridge to function
            WebView.IsJavaScriptEnabled = true;
            WebView.IsScriptNotifyAllowed = true;

            IsActive = false;
        }

        /// <summary>
        ///     Gets or sets a safe list of Uniform Resource Identifiers (URIs) that are permitted to fire
        ///     <see cref="IWebView.ScriptNotify" /> events to the <see cref="IWebView" /> for consumption by the
        ///     <see cref="JavaScriptBridge" />
        /// </summary>
        public ICollection<Uri> AllowedScriptNotifyUris { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is actively listening to <see cref="IWebView.ScriptNotify"/> events..
        /// </summary>
        /// <value><see langword="true" /> if this instance is active; otherwise, <see langword="false" />.</value>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets or sets the .NET delegates that may be invoked from JavaScript.
        /// </summary>
        /// <value>The scripting delegates.</value>
        protected Dictionary<string, Func<Dictionary<string, object>, object>> ScriptingDelegates { get; }

        /// <summary>
        /// Gets the <see cref="IWebView"/> instance.
        /// </summary>
        /// <value>The web view.</value>
        protected IWebView WebView { get; }

        /// <summary>
        /// Creates an instance of <see cref="JavaScriptBridge"/> and starts listening to <see cref="IWebView.ScriptNotify"/> messages.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView"/> instance.</param>
        /// <param name="allowedOrigins">The set of allowed origins for <see cref="IWebView.ScriptNotify"/> events.</param>
        /// <returns>An instance of <see cref="JavaScriptBridge" />.</returns>
        /// <exception cref="ArgumentNullException">
        /// Occurs when <paramref name="webView"/> or  <paramref name="allowedOrigins" /> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">allowedOrigins - Must have at least one origin.</exception>
        /// <seealso cref="Start"/>
        /// <seealso cref="AllowedScriptNotifyUris"/>
        public static JavaScriptBridge CreateAndStart(
            IWebView webView,
            params Uri[] allowedOrigins
        )
        {

            if (allowedOrigins == null)
            {
                throw new ArgumentNullException(nameof(allowedOrigins));
            }

            if (!allowedOrigins.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(allowedOrigins), "Must have at least one origin.");
            }

            var retval = new JavaScriptBridge(webView);

            foreach (var origin in allowedOrigins)
            {
                retval.AllowedScriptNotifyUris.Add(origin);
            }

            retval.Start();

            return retval;
        }

        /// <summary>
        /// Adds the scripting delegate with a given <paramref name="handlerId"/>.
        /// </summary>
        /// <param name="handlerId">The identity of the delegate to be used by JavaScript.</param>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>
        /// The <paramref name="action"/> is a callback to be performed when invoking the delegate by id. The callback accepts a named parameter collection
        /// via <see cref="Dictionary{String,Object}"/> and permits a return of <see cref="Object"/>. The callback itself need not accept any parameters, nor
        /// need it return a value. If no parameters are passed, an empty <see cref="Dictionary{String,Object}"/> is passed for the parameters. If the callback
        /// does not produce a return value, <see langword="null" /> is returned.
        /// </remarks>
        public void AddScriptingHandler(string handlerId, Func<Dictionary<string, object>, object> action)
        {
            ScriptingDelegates.Add(handlerId, action);
        }

        /// <summary>
        /// When overridden in a class, provides custom logic to be executed after <see cref="Start"/>.
        /// </summary>
        /// <seealso cref="Start"/>.
        /// <remarks>This implementation ensures the bridge is loaded by handling <see cref="IWebView.DOMContentLoaded"/> and the
        /// <see cref="IWebView.Source"/> is a valid <see cref="AllowedScriptNotifyUris"/>.</remarks>
        public virtual void OnStarted()
        {
            WebView.DOMContentLoaded += OnWebViewDOMContentLoaded;
        }

        /// <summary>
        /// When overridden in a class, provides custom logic to be executed after <see cref="Stop"/>.
        /// </summary>
        /// <seealso cref="Stop"/>.
        /// <remarks>Stops listening for <see cref="IWebView.DOMContentLoaded"/>.</remarks>
        /// <seealso cref="OnStarted"/>
        public virtual void OnStopped()
        {
            WebView.DOMContentLoaded -= OnWebViewDOMContentLoaded;
        }

        /// <summary>
        /// Starts listening for <see cref="IWebView.ScriptNotify"/> events.
        /// </summary>
        /// <seealso cref="IWebView.ScriptNotify"/>
        /// <seealso cref="AllowedScriptNotifyUris"/>
        /// <seealso cref="IsActive"/>
        /// <seealso cref="OnStarted"/>
        public void Start()
        {
            if (!IsActive)
            {
                IsActive = true;

                WebView.ScriptNotify += OnWebViewScriptNotify;

                OnStarted();
            }
        }


        /// <summary>
        /// Stops listening for <see cref="IWebView.ScriptNotify"/> events.
        /// </summary>
        /// <seealso cref="IsActive"/>
        /// <seealso cref="IWebView.ScriptNotify"/>
        /// <seealso cref="OnStopped"/>
        public void Stop()
        {
            if (IsActive)
            {
                IsActive = false;

                WebView.ScriptNotify -= OnWebViewScriptNotify;

                OnStopped();
            }
        }

        /// <summary>
        /// Handle the <see cref="IWebView.DOMContentLoaded" /> event, ensuring the JavaScript bridge is loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WebViewControlDOMContentLoadedEventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// The bridge can be loaded by
        ///   - Explicitly using a script tag in the markup of the page, or
        ///   - Explicitly through handling the navigation or DOM events
        ///
        /// If the JavaScript bridge is already loaded, it will not be loaded again.
        /// </remarks>
        /// <seealso cref="AllowedScriptNotifyUris"/>
        /// <seealso cref="LoadBridge"/>
        private void OnWebViewDOMContentLoaded(object sender, WebViewControlDOMContentLoadedEventArgs e)
        {
            if (e.Uri != null &&
                AllowedScriptNotifyUris != null &&
                AllowedScriptNotifyUris.Contains(e.Uri) &&
                sender is IWebView webView)
            {
                LoadBridge();
            }
        }

        /// <summary>
        ///     Loads the JavaScript bridge into a <paramref name="webView" /> instance if not already loaded.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <remarks>
        ///     Bridge is not loaded unless the navigation was successful.
        /// </remarks>
        public async Task LoadBridge()
        {
            Trace.WriteLine("Executing JavaScript to detect presence of JavaScriptBridge", Constants.TraceName);

            var result = await WebView.InvokeScriptAsync("eval", "typeof JavaScriptBridge == 'object'");
            if (!"true".Equals(result))
            {
                // NOT already loaded. Load from disk
                Trace.WriteLine("Could not detect JavaScriptBridge. Injecting script", Constants.TraceName);

                var contents = File.ReadAllText(Constants.BridgeJavaScript);
                WebView.Eval(contents);
            }
        }

        /// <summary>
        /// Handles the <see cref="IWebView.ScriptNotify" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WebViewControlScriptNotifyEventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// Since there may be other uses of <code>window.external.notify</code> in use, this method checks to see if the
        /// message came from a URI within <see cref="AllowedScriptNotifyUris"/> and the bridge before processing it. If it did,
        /// it triggers processing of all queued messages within the bridge asynchronously.
        /// </remarks>
        /// <seealso cref="AllowedScriptNotifyUris"/>
        /// <seealso cref="IsJavaScriptBridgeMessage"/>
        /// <seealso cref="FlushAsync"/>
        private void OnWebViewScriptNotify(object sender, WebViewControlScriptNotifyEventArgs e)
        {
            Trace.WriteLine($"Entering {typeof(JavaScriptBridge).FullName}.{nameof(OnWebViewScriptNotify)}", Constants.TraceName);

            // White list
            // WinForms/WPF do not validate the origins for ScriptNotify like UWP does
            // This needs to be done manually for security
            if (e.Uri != null && AllowedScriptNotifyUris != null && AllowedScriptNotifyUris.Contains(e.Uri))
            {
                var message = e.Value;

                if (sender is IWebView webView && IsJavaScriptBridgeMessage(message))
                {
                    Trace.WriteLine($"Received command {message}", Constants.TraceName);

                    // Asynchronously flush the messages from the JavaScript bridge
                    FlushAsync();
                }
            }
            else
            {
                Trace.WriteLine($"{nameof(IWebView.ScriptNotify)} received from unknown origin {e.Uri?.Host}", Constants.TraceName);
            }
        }

        /// <summary>
        ///     Determines if the <paramref name="message" /> is from the JavaScript bridge.
        /// </summary>
        /// <param name="message">The contents of <see cref="WebViewControlScriptNotifyEventArgs.Value" />.</param>
        /// <returns><see langword="true" /> if message originated from JavaScript bridge; otherwise, <see langword="false" />.</returns>
        private bool IsJavaScriptBridgeMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                // If using the helper methods "sendData" and "callNative" a message will fall into this
                if (message.StartsWith("jsbridge", StringComparison.OrdinalIgnoreCase))
                {
                    if (message.IndexOf("queue_message", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                // If pushing a message directly, it will be this
                // Need to make sure in this case it is not malformed
                JToken token = null;

                try
                {
                    token = JsonConvert.DeserializeObject<JToken>(message);
                }
                catch (Exception)
                {
                }

                if (token?[nameof(JavaScriptBridgeMessage.Handler)] != null)
                {
                    var delegateName = token[nameof(JavaScriptBridgeMessage.Handler)].Value<string>();
                    if (!string.IsNullOrEmpty(delegateName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Flushes all queued messages from the JavaScript bridge
        /// </summary>
        /// <returns>A <see cref="Task"/>.</returns>
        /// <exception cref="NotSupportedException">If the requested callback identifier is not in the list of <see cref="ScriptingDelegates"/>.</exception>
        /// <seealso cref="ScriptingDelegates"/>
        /// <remarks>
        /// If an exception is encountered while processing the handler it is captured and returned to the caller via <see cref="JavaScriptBridgeMessage.ErrorData"/>.
        /// Each message is handled independently and dispatched back to the bridge in the order in which they are received.
        /// </remarks>
        private async Task FlushAsync()
        {
            {
                Trace.WriteLine($"Calling {Constants.BridgeJavaScript} method JavaScriptBridge.fetchQueue()",
                    Constants.TraceName);

                List<JavaScriptBridgeMessage> messages;

                try
                {
                    messages = await WebView.InvokeScriptFunctionAsync<List<JavaScriptBridgeMessage>>(
                        "JavaScriptBridge",
                        "fetchQueue");
                }
                catch (Exception)
                {
                    // TODO: Catch a more specific exception here
                    throw;
                }

                if (messages != null)
                {
                    Trace.WriteLine($"Retrieved {messages.Count} message{(messages.Count != 1 ? "s" : string.Empty)}",
                        Constants.TraceName);

                    foreach (var message in messages)
                    {
                        try
                        {
                            var handlerName = message.Handler;
                            if (ScriptingDelegates.ContainsKey(handlerName))
                            {
                                message.ResponseData = ScriptingDelegates[handlerName](message.HandlerData);
                            }
                            else
                            {
                                throw new NotSupportedException($"Handler '{handlerName}' not supported.");
                            }
                        }
                        catch (Exception exception)
                        {
                            Trace.WriteLine(
                                $"Error while processing message: [{exception.GetType()}] {exception.Message}",
                                Constants.TraceName);

                            // Encountered an excpetion while processing the message
                            // Mask any potential disclosure issues by wrapping the exception as our own
                            var e =
                                new JavaScriptBridgeException(exception.Message)
                                {
                                    Source = Constants.TraceName
                                };
                            message.ErrorData = e;
                        }
                        finally
                        {
                            if (!string.IsNullOrEmpty(message.CallbackId))
                            {
                                WebView.DispatchMessage(message);
                            }
                        }
                    }
                }
            }
        }
    }
}