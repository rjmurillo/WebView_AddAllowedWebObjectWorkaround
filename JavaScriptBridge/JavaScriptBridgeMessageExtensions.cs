using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions
{
    /// <summary>
    /// Contains extension methods for <see cref="JavaScriptBridgeMessage"/>.
    /// </summary>
    public static class JavaScriptBridgeMessageExtensions
    {
        /// <summary>
        /// Serialization settings for Newtonsoft JSON.Net to produce JSON that JavaScript will be able to parse
        /// </summary>
        public static JsonSerializerSettings MessageSerializationSettings { get; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            Formatting = Formatting.None
        };

        /// <summary>
        /// Serializes the specified <paramref name="javaScriptBridgeMessage"/> to JSON.
        /// </summary>
        /// <param name="javaScriptBridgeMessage">A <see cref="JavaScriptBridgeMessage"/>.</param>
        /// <returns>JSON representation of the <paramref name="javaScriptBridgeMessage"/></returns>
        public static string Serialize(this JavaScriptBridgeMessage javaScriptBridgeMessage)
        {
            return JsonConvert.SerializeObject(javaScriptBridgeMessage, MessageSerializationSettings);
        }
    }
}
