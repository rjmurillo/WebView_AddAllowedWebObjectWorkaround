using System.Collections.Generic;

namespace Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions
{
    /// <summary>
    /// A data contract to move messages on the JavaScript bridge between JavaScript and .NET
    /// </summary>
    public class JavaScriptBridgeMessage
    {
        /// <summary>
        /// Gets or sets the callback.
        /// </summary>
        /// <value>The callback identifier to be used when the message is passed back to the JavaScript bridge.</value>
        /// <remarks>This is used internally to ensure the proper JavaScript callbacks are invoked when resuming execution within the JavaScript bridge.</remarks>
        public string CallbackId { get; set; }

        /// <summary>
        /// Gets or sets the error data.
        /// </summary>
        /// <value>The error data.</value>
        /// <remarks>This property is only set when an error occurs while processing the message.</remarks>
        public JavaScriptBridgeException ErrorData { get; set; }

        /// <summary>
        /// Gets or sets the handler to be used to process the message.
        /// </summary>
        /// <value>The handler.</value>
        /// <seealso cref="JavaScriptBridge.ScriptingDelegates"/>
        public string Handler { get; set; }

        /// <summary>
        /// Gets or sets the parameters to be used by the <see cref="Handler"/>.
        /// </summary>
        /// <value>The named parameters as <see cref="Dictionary{String,Object}"/>.</value>
        public Dictionary<string,object> HandlerData { get; set; }

        /// <summary>
        /// Gets or sets the response data.
        /// </summary>
        /// <value>An optional return object to be used by the JavaScript callback as a result of invoking the <see cref="Handler"/>.</value>
        public object ResponseData { get; set; }
    }
}
