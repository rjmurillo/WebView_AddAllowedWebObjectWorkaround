using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Toolkit.Win32.UI.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions;
using WebViewAddAllowedWebObjectWorkaround.Shared;

namespace WebViewAddAllowedWebObjectWorkaround
{
    public partial class Form1 : Form
    {
        private readonly JavaScriptBridge _javaScriptBridge;
        private readonly ProductRepository _productRepository;

        public Form1()
        {
            InitializeComponent();

            // Initialize the product repository
            _productRepository = new ProductRepository();

            // Initialize the JavaScriptBridge
            _javaScriptBridge = JavaScriptBridge.CreateAndStart(webView1, Constants.PermittedOrigin);
            _javaScriptBridge.AddScriptingHandler("GetAllProducts", @params => _productRepository.GetProducts());
            _javaScriptBridge.AddScriptingHandler("GetProduct", @params =>
            {
                // Since these are coming from JavaScript, take extra care to validate the parameters
                if (@params != null && @params.Count == 1 && @params.ContainsKey("name"))
                {
                    var name = @params["name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        return _productRepository.GetProductByName(name);
                    }
                }

                throw new ArgumentException("Unexpected parameters");
            });

            webView1.DOMContentLoaded += (o, e) => Text = webView1.DocumentTitle;

            // This is a custom text writer trace listener to send diagnostic data to the webview log
            Trace.Listeners.Add(new WebViewTraceListener(webView1));

            // When the page loads push the product data to the page and bind with JavaScript showData function
            webView1.NavigationCompleted += (o, e) =>
            {
                if (o is IWebView wv)
                {
                    // Get the products and push to JavaScript function "showData"
                    var products = new ProductRepository().GetProducts();
                    wv.InvokeScriptFunctionAsync("showData", products);
                }
            };

            // Perform the navigation as you normally would. This could either by through the Source property,
            // or through one of the navigation methods.
            //
            // Using the method to navigate to local content for our example
            webView1.NavigateToLocal("/Example.html");
        }
    }
}
