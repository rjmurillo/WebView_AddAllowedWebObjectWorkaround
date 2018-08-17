using System;
using System.Diagnostics;
using System.Windows;
using Microsoft.Toolkit.Win32.UI.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions;
using WebViewAddAllowedWebObjectWorkaround.Shared;


namespace WebViewAddAllowedWebObjectWorkaround_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly JavaScriptBridge _javaScriptBridge;
        private readonly ProductRepository _productRepository;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the product repository
            _productRepository = new ProductRepository();

            // Initialize the JavaScriptBridge
            _javaScriptBridge = JavaScriptBridge.CreateAndStart(WebView, Constants.PermittedOrigin);
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

            WebView.DOMContentLoaded += (o, e) => Title = WebView.DocumentTitle;

            // This is a custom text writer trace listener to send diagnostic data to the webview log
            Trace.Listeners.Add(new WebViewTraceListener(WebView));


            // When the page loads push the product data to the page and bind with JavaScript showData function
            WebView.NavigationCompleted += (o, e) =>
            {
                if (o is IWebView wv)
                {
                    // Get the products and push to JavaScript function "showData"
                    var products = new ProductRepository().GetProducts();
                    wv.InvokeScriptFunctionAsync("showData", products);
                }
            };
        }

        private void WebView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Perform the navigation as you normally would. This could either by through the Source property,
            // or through one of the navigation methods.
            //
            // Using the method to navigate to local content for our example
            WebView.NavigateToLocal("/Example.html");
        }
    }
}
