namespace JavaScriptBridge
{
    /// <summary>
    /// A data contract to move messages on the JavaScript bridge between JavaScript and C#
    /// </summary>
    public class JavaScriptBridgeMessage
    {
        /// <summary>
        /// Gets or sets the handler to be used to process the message.
        /// </summary>
        /// <value>The handler.</value>
        public string Handler { get; set; }
        /// <summary>
        /// Gets or sets the data to be used by the <see cref="Handler"/>.
        /// </summary>
        /// <value>The data as JSON.</value>
        public string HandlerData { get; set; }
        /// <summary>
        /// Gets or sets the callback.
        /// </summary>
        /// <value>The callback identifier to be used when the message is passed back to the JavaScript bridge.</value>
        public string CallbackId { get; set; }
        /// <summary>
        /// Gets or sets the response data.
        /// </summary>
        /// <value>An optional object to be used by the JavaScript callback.</value>
        public object ResponseData { get; set; }
    }
}