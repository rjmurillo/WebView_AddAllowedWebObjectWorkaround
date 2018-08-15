using System;
using System.Collections.Generic;
using System.Diagnostics;
using JavaScriptBridge;
using Newtonsoft.Json;

namespace WebViewAddAllowedWebObjectWorkaround.Shared
{
    public static class WebViewMessageHandler
    {
        // Contains sample "Products" to send back to the WebView
        public static ProductRepository Repository { get; } = new ProductRepository();

        // Method responsible for actually processing the messages.
        public static void MessageHandler(JavaScriptBridgeMessage javaScriptBridgeMessage)
        {
            Trace.WriteLine($"Entering {typeof(WebViewMessageHandler).FullName}.{nameof(MessageHandler)}");
            Trace.WriteLine(javaScriptBridgeMessage, "Processing message");

            // Determine if JavaScript has requested a .NET handler
            // The handler could be anything you want. In this example I've chosen names of methods
            if (!string.IsNullOrEmpty(javaScriptBridgeMessage.Handler))
            {
                // JavaScript has requested a handler for the message.
                // Since we're using names for the example, check each known method exposed and send to the appropriate location
                if ("GetAllProducts".Equals(javaScriptBridgeMessage.Handler, StringComparison.OrdinalIgnoreCase))
                {
                    // The "GetProducts" method takes no arguments, so just call it and set the return value in ResponseData
                    javaScriptBridgeMessage.ResponseData = Repository.GetProducts();
                }
                else if ("GetProduct".Equals(javaScriptBridgeMessage.Handler, StringComparison.OrdinalIgnoreCase))
                {
                    // "GetProduct" takes arguments. Deserialize the arguments from the "HandlerData" property
                    // on the JavaScriptBridgeMessage.
                    //
                    // Deserialized here as a collection of Object. You'll want to add validation here
                    // for the number of arguments, order, values, etc.
                    var args = JsonConvert.DeserializeObject<List<object>>(javaScriptBridgeMessage.HandlerData);
                    javaScriptBridgeMessage.ResponseData = Repository.GetProductByName(args[0].ToString());
                }
                else
                {
                    // An example of how we can return exceptions back to JavaScript through the bridge
                    // It is important to strip any information from an exception to avoid disclosure to attacker
                    throw new NotSupportedException($"{javaScriptBridgeMessage.Handler} not supported.");
                }
            }
        }
    }
}