using JavaScriptBridge;

using Microsoft.Toolkit.Win32.UI.Controls.WinForms;

using Newtonsoft.Json;

using System;
using System.Diagnostics;

namespace WebViewAddAllowedWebObjectWorkaround
{
    public class WebViewTraceListener : TraceListener
    {
        private readonly WebView _webView;

        public WebViewTraceListener(WebView webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        public override void Write(string message) => WriteLine(message);

        public override void Write(object o, string category)
        {
            WriteLine(o, category);
        }

        public override void Write(string message, string category)
        {
            WriteLine(message, category);
        }

        public override void Write(object o)
        {
            Write(o, string.Empty);
        }

        public override void WriteLine(string message)
        {
            Eval($"log('{FormatCategory("C#")} {message}');");
        }

        public override void WriteLine(string message, string category)
        {
            WriteLine($"{FormatCategory(category)} {message}".TrimStart());
        }

        public override void WriteLine(object o)
        {
            WriteLine(o, string.Empty);
        }

        public override void WriteLine(object o, string category)
        {
            Eval($@"
var obj = {JsonConvert.SerializeObject(o, JavaScriptBridgeMessageExtensions.MessageSerializationSettings)};
log('{FormatCategory("C#")} {FormatCategory(category)}', obj);");
        }
        private void Eval(string script)
        {
            try
            {
                _webView.InvokeScriptAsync("eval", script);
            }
            catch (Exception)
            {
                // Best effort
            }
        }

        private string FormatCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return string.Empty;
            }

            return $"<span class=\"badge\">{category}</span>";
        }
    }
}
