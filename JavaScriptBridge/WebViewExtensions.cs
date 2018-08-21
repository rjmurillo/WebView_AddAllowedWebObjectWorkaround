using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions
{
    /// <summary>
    ///     Contains extension methods for <see cref="IWebView" />
    /// </summary>
    public static class WebViewExtensions
    {
        /// <summary>
        ///     Dispatch a <see cref="JavaScriptBridgeMessage" />.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="javaScriptBridgeMessage">The message.</param>
        internal static void DispatchMessage(this IWebView webView, JavaScriptBridgeMessage javaScriptBridgeMessage)
        {
            var json = javaScriptBridgeMessage?.Serialize();

            if (!string.IsNullOrEmpty(json))
            {
                Trace.WriteLine(
                    $"Calling {Constants.BridgeJavaScript} JavaScriptBridge.handleNativeMessage",
                    Constants.TraceName);
                Trace.WriteLine(javaScriptBridgeMessage, Constants.TraceName);

                // Need to sanitize message sent to bridge
                
                // Escape any single quotes, since that's what we're using
                json = json.Replace("'", "\\'");

                // Escape any CL/LF
                json = json.Replace(Environment.NewLine, "\\\\n");

                json = json.Replace("\n", "\\\\n");

                var command = $"JavaScriptBridge.handleNativeMessage('{json}');";
                // in WPF ensure UI access
                webView.TryEvalAsync(command);
            }
        }

        /// <summary>
        ///     Performs asynchronous JavaScript eval without waiting for return.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="script">The script to evaluate.</param>
        internal static void Eval(this IWebView webView, string script)
        {
            webView?.EvalAsync(script);
        }

        /// <summary>
        ///     Performs asynchronous JavaScript eval using <see cref="IWebView.InvokeScriptAsync(string, string[])" />.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="script">The script to evaluate.</param>
        /// <returns>A <see cref="Task{String}" />.</returns>
        internal static Task<string> EvalAsync(this IWebView webView, string script)
        {
            return webView?.InvokeScriptAsync("eval", script);
        }

        /// <summary>
        ///     Executes the given JavaScript function within the "window" scope. If the JavaScript function returns a value, it is
        ///     returned as <see cref="JToken" />.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="function">The name of the JavaScript function in the module.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>A <see cref="JToken" />.</returns>
        [DebuggerStepThrough]
        public static Task<JToken> InvokeJavaScriptFunctionWithJTokenResponse(
            this IWebView webView,
            string function,
            params object[] args)
        {
            return InvokeJavaScriptFunctionWithJTokenResponse(webView, "window", function, args);
        }

        /// <summary>
        ///     Executes the given JavaScript function within the specified scope. If the JavaScript function returns a value, it
        ///     is returned as <see cref="JToken" />.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="module">The JavaScript module (e.g. window).</param>
        /// <param name="function">The name of the JavaScript function in the module.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>A <see cref="JToken" />.</returns>
        public static async Task<JToken> InvokeJavaScriptFunctionWithJTokenResponse(
            IWebView webView,
            string module,
            string function,
            params object[] args)
        {
            var result = await InvokeScriptFunctionAsync(webView, module, function, args);
            return JsonConvert.DeserializeObject<JToken>(result);
        }

        /// <summary>
        ///     Executes the given JavaScript function within the "window" scope. If the JavaScript function returns a value, it is
        ///     returned as JSON.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="function">The name of the JavaScript function in the module.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>If the function returns a value, JSON representation of that value.</returns>
        /// <seealso cref="InvokeScriptFunctionAsync(IWebView,string,string,object[])" />
        [DebuggerStepThrough]
        public static Task<string> InvokeScriptFunctionAsync(this IWebView webView, string function,
            params object[] args)
        {
            return InvokeScriptFunctionAsync(webView, "window", function, args);
        }

        /// <summary>
        ///     Executes the given JavaScript function in the specified module with arguments. If the JavaScript function returns a
        ///     value, it is returned as JSON.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="module">The JavaScript module (e.g. window).</param>
        /// <param name="function">The name of the JavaScript function in the module.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>If the function returns a value, JSON representation of that value.</returns>
        public static Task<string> InvokeScriptFunctionAsync(this IWebView webView, string module, string function,
            params object[] args)
        {
            var builder = new StringBuilder();
            builder.AppendLine("(function() {");
            builder.AppendFormat("  if ({0} && {0}.{1}) {{\r\n", module, function);
            builder.AppendFormat("    return JSON.stringify({0}.{1}(", module, function);

            args = args ?? Array.Empty<object>();

            var builder2 = new StringBuilder();
            var i = 0;
            while (true)
            {
                if (i >= args.Length)
                {
                    builder.Append(builder2);
                    builder.AppendLine("));");
                    builder.AppendLine("  }");
                    builder.Append("})();");

                    break;
                }

                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder2.Append(
                    JsonConvert.SerializeObject(
                        args[i],
                        JavaScriptBridgeMessageExtensions.MessageSerializationSettings));
                i++;
            }

            return EvalAsync(webView, builder.ToString());
        }

        /// <summary>
        ///     Executes the given JavaScript function within the "window" scope. If the JavaScript function returns a value, it is
        ///     returned as an instance of
        ///     <typeparamref name="T" />
        ///     .
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="function">The name of the JavaScript function in the module.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        ///     An instance of      <typeparamref name="T" />
        ///     .
        /// </returns>
        [DebuggerStepThrough]
        public static Task<T> InvokeScriptFunctionAsync<T>(
            this IWebView webView,
            string function,
            params object[] args)
        {
            return InvokeScriptFunctionAsync<T>(webView, "window", function, args);
        }

        /// <summary>
        ///     Executes the given JavaScript function within the specified module. If the JavaScript function returns a value, it
        ///     is returned as an instance of
        ///     <typeparamref name="T" />
        ///     .
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="module">The JavaScript module (e.g. window).</param>
        /// <param name="function">The name of the JavaScript function in the module.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        ///     An instance of
        ///     <typeparamref name="T" />
        ///     .
        /// </returns>
        public static async Task<T> InvokeScriptFunctionAsync<T>(
            this IWebView webView,
            string module,
            string function,
            params object[] args)
        {
            var result = await InvokeScriptFunctionAsync(webView, module, function, args);

            if (string.IsNullOrEmpty(result))
            {
                return default(T);
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(result);
            }
            catch (JsonReaderException)
            {
                throw;
            }

            return default(T);
        }

        /// <summary>
        ///     Executes the given JavaScript function within the "window" scope.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="function">The name of the JavaScript function in the module.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        [DebuggerStepThrough]
        public static Task<Dictionary<string, object>> InvokeScriptFunctionWithDicionaryResponseAsync(
            this IWebView webView,
            string function,
            params object[] args)
        {
            return InvokeScriptFunctionWithDicionaryResponseAsync(webView, "window", function, args);
        }

        /// <summary>
        ///     Executes the given JavaScript function within the specified scope.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
        /// <param name="module">The JavaScript module (e.g. window).</param>
        /// <param name="function">The name of the JavaScript function in the module.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        public static async Task<Dictionary<string, object>> InvokeScriptFunctionWithDicionaryResponseAsync(
            this IWebView webView,
            string module,
            string function,
            params object[] args)
        {
            var result = await InvokeScriptFunctionAsync(webView, module, function, args);
            return !string.IsNullOrEmpty(result)
                ? JsonConvert.DeserializeObject<Dictionary<string, object>>(result)
                : new Dictionary<string, object>(0);
        }

        /// <summary>
        ///     Attempts to asynchronously evaluate the <paramref name="script" /> in the <paramref name="webView" /> without
        ///     waiting for return.
        /// </summary>
        /// <param name="webView">A <see cref="IWebView" /> instance.</param>
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