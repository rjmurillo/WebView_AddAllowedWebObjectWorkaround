using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Microsoft.Toolkit.Win32.UI.Controls.WinForms;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using JavaScriptBridge;

namespace WebViewAddAllowedWebObjectWorkaround
{
    public partial class Form1 : Form
    {
        private readonly ProductRepository _productRepository;
        private static readonly Uri PermittedOrigin = new Uri(
            "ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Example.html");

        public Form1()
        {
            InitializeComponent();

            webView1.DOMContentLoaded += (o, e) => Text = webView1.DocumentTitle;

            // Contains sample "Products" to send back to the WebView
            _productRepository = new ProductRepository();

            // This is a custom text writer trace listener to send diagnostic data to the webview log
            Trace.Listeners.Add(new WebViewTraceListener(webView1));

            // Ensure the following properties are set. They can be set programatically, or through the designer
            webView1.IsScriptNotifyAllowed = true;
            webView1.IsJavaScriptEnabled = true;

            // Ensure the JavaScript bridge is loaded. It can be loaded:
            //  - Explicitly by the remote resource by including it in the markup from a HTTP server, or
            //  - Explicitly through C# by handling the NavigationCompleted event.
            //
            // NOTE: If the JavaScript bridge is already loaded, it will not be loaded again.
            webView1.NavigationCompleted += OnWebViewNavigationCompleted;

            // Perform the navigation as you normally would. This could either by through the Source property,
            // or through one of the navigation methods.
            //
            // Using the method to navigate to local content for our example
            webView1.NavigateToLocal("/Example.html");
        }

        // Handle the WebView's NavigationCompleted event, ensuring the JavaScript bridge is loaded
        // This method also adds a method to subscribe to the ScriptNotify event, which will be triggered
        // by the JavaScript bridge
        private void OnWebViewNavigationCompleted(object o, WebViewControlNavigationCompletedEventArgs e)
        {
            // White list
            // We only load the JavaScript bridge into origins we trust
            if (e.Uri == PermittedOrigin)
            {
                ((WebView) o).LoadBridge(e);
                webView1.ScriptNotify += OnWebViewScriptNotify;
            }
            else
            {
                Trace.WriteLine($"Navigation to unknown origin {e.Uri.Host}");
            }
        }

        // Handles the ScriptNotify event for the WebView. Since there may be other uses of
        // window.external.notify at play, this method checks to see if the message came from
        // the bridge before processing it. If it did, trigger processing of all queued
        // messages within the bridge.
        private void OnWebViewScriptNotify(object sender, WebViewControlScriptNotifyEventArgs e)
        {
            Trace.WriteLine($"Entering {GetType().FullName}.{nameof(OnWebViewScriptNotify)}");

            // White list
            // WinForms/WPF do not validate the origins for ScriptNotify like UWP does
            // This needs to be done manually for security
            if (e.Uri == PermittedOrigin)
            {
                var message = e.Value;

                if (sender is WebView webView && webView.IsJavaScriptBridgeMessage(message))
                {
                    Trace.WriteLine($"Received command {message}");
                    // Asynchronously flush the messages from the JavaScript bridge using the
                    // method called "MessageHandler"
                    webView.FlushAsync(MessageHandler);
                }
            }
            else
            {
                Trace.WriteLine($"{nameof(WebView.ScriptNotify)} received from unknown origin {e.Uri.Host}");
            }
        }

        // Method responsible for actually processing the messages.
        private void MessageHandler(JavaScriptBridgeMessage javaScriptBridgeMessage)
        {
            Trace.WriteLine($"Entering {GetType().FullName}.{nameof(MessageHandler)}");
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
                    javaScriptBridgeMessage.ResponseData = _productRepository.GetProducts();
                }
                else if ("GetProduct".Equals(javaScriptBridgeMessage.Handler, StringComparison.OrdinalIgnoreCase))
                {
                    // "GetProduct" takes arguments. Deserialize the arguments from the "HandlerData" property
                    // on the JavaScriptBridgeMessage.
                    //
                    // Deserialized here as a collection of Object. You'll want to add validation here
                    // for the number of arguments, order, values, etc.
                    var args = JsonConvert.DeserializeObject<List<object>>(javaScriptBridgeMessage.HandlerData);
                    javaScriptBridgeMessage.ResponseData = _productRepository.GetProductByName(args[0].ToString());
                }

                // Null the data out to avoid dealing with serialization issues in DispatchMessage
                javaScriptBridgeMessage.HandlerData = null;
            }
        }
    }

    // A sample plain old CLR object (POCO)
    public class Product
    {
        public string Name { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal Price { get; set; }
        public string[] Sizes { get; set; }
    }

    // A sample data provider, providing instances of class Product to be used by JaavaScriptInteropMethods class
    public class ProductRepository
    {
        private static readonly Dictionary<string, Product> Products = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase)
        {
            {"Apple", new Product()
                {
                    Name = "Apple",
                    ExpiryDate = DateTime.Today.AddDays(3d),
                    Price = 3.99M,
                    Sizes = new []{"Small", "Medium", "Large"}
                }
            },
            {"Pear", new Product()
                {
                    Name = "Pear",
                    ExpiryDate = DateTime.Today.AddDays(7d),
                    Price = 2.29M,
                    Sizes = new []{"Small", "Large"}
                }
            }
        };

        public IEnumerable<Product> GetProducts()
        {
            Trace.WriteLine($"Entering {GetType().FullName}.{nameof(GetProducts)}");
            return Products.Values;
        }

        public Product GetProductByName(string name)
        {
            Trace.WriteLine($"Entering {GetType().FullName}.{nameof(GetProductByName)}");
            Products.TryGetValue(name, out var retval);
            return retval;
        }
    }
}
