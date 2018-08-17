using System;
using Microsoft.Toolkit.Win32.UI.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions;

namespace WebViewAddAllowedWebObjectWorkaround.Shared
{
    /// <summary>
    /// Contains constant values for sample
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// URI of permissable <see cref="IWebView.ScriptNotify"/> events.
        /// </summary>
        /// <seealso cref="JavaScriptBridge.AllowedScriptNotifyUris"/>
        public static readonly Uri PermittedOrigin = new Uri(
            "ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Example.html");
    }
}
