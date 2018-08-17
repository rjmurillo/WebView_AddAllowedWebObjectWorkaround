using System;

namespace Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions
{
    /// <summary>
    /// Represents errors that my occur while processing a <see cref="JavaScriptBridgeMessage"/>.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class JavaScriptBridgeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptBridgeException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public JavaScriptBridgeException(string message)
            : base(message)
        {
        }
    }
}
