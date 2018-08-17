using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Toolkit.Win32.UI.Controls;
using Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions;
using Newtonsoft.Json;

namespace WebViewAddAllowedWebObjectWorkaround.Shared
{
    /// <summary>
    /// Provides a class to send debug and trace output to the sample page's logger
    /// </summary>
    /// <seealso cref="System.Diagnostics.TraceListener" />
    public class WebViewTraceListener : TraceListener
    {
        private static readonly string CSharpCategory = FormatCategory("C#");
        private readonly IWebView _webView;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebViewTraceListener"/> class.
        /// </summary>
        /// <param name="webView">The web view.</param>
        /// <exception cref="ArgumentNullException">webView if null</exception>
        public WebViewTraceListener(IWebView webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        /// <summary>
        /// When overridden in a derived class, writes the specified message to the listener you create in the derived class.
        /// </summary>
        /// <param name="message">A message to write.</param>
        /// <seealso cref="WriteLine(string)"/>
        public override void Write(string message)
        {
            WriteLine(message);
        }

        /// <summary>
        /// Writes a category name and the value of the object's <see cref="M:System.Object.ToString" /> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object" /> whose fully qualified class name you want to write.</param>
        /// <param name="category">A category name used to organize the output.</param>
        /// <seealso cref="WriteLine(object, string)"/>
        public override void Write(object o, string category)
        {
            WriteLine(o, category);
        }

        /// <summary>
        /// Writes a category name and a message to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class.
        /// </summary>
        /// <param name="message">A message to write.</param>
        /// <param name="category">A category name used to organize the output.</param>
        /// <seealso cref="WriteLine(string, string)"/>
        public override void Write(string message, string category)
        {
            WriteLine(message, category);
        }

        /// <summary>
        /// Writes the value of the object's <see cref="M:System.Object.ToString" /> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object" /> whose fully qualified class name you want to write.</param>
        /// <seealso cref="Write(object, string)"/>
        public override void Write(object o)
        {
            Write(o, string.Empty);
        }

        /// <summary>
        /// When overridden in a derived class, writes a message to the listener you create in the derived class, followed by a line terminator.
        /// </summary>
        /// <param name="message">A message to write.</param>
        /// <seealso cref="Eval(string, string, object)"/>
        public override void WriteLine(string message)
        {
            Eval(null, message);
        }

        /// <summary>
        /// Writes a category name and a message to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class, followed by a line terminator.
        /// </summary>
        /// <param name="message">A message to write.</param>
        /// <param name="category">A category name used to organize the output.</param>
        /// <seealso cref="Eval(string, string, object)"/>
        public override void WriteLine(string message, string category)
        {
            Eval(category, message);
        }

        /// <summary>
        /// Writes the value of the object's <see cref="M:System.Object.ToString" /> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class, followed by a line terminator.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object" /> whose fully qualified class name you want to write.</param>
        /// <seealso cref="WriteLine(object, string)"/>
        public override void WriteLine(object o)
        {
            WriteLine(o, string.Empty);
        }

        /// <summary>
        /// Writes a category name and the value of the object's <see cref="M:System.Object.ToString" /> method to the listener you create when you implement the <see cref="T:System.Diagnostics.TraceListener" /> class, followed by a line terminator.
        /// </summary>
        /// <param name="o">An <see cref="T:System.Object" /> whose fully qualified class name you want to write.</param>
        /// <param name="category">A category name used to organize the output.</param>
        /// <seealso cref="Eval(string, string, object)"/>
        public override void WriteLine(object o, string category)
        {
            Eval(category, null, o);
        }

        /// <summary>
        /// Formats the category into HTML for the logger.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>HTML for the <paramref name="category"/>.</returns>
        private static string FormatCategory(string category)
        {
            return string.IsNullOrWhiteSpace(category)
                ? string.Empty
                : $"<span class=\"badge\">{category}</span>";
        }

        private void Eval(string category, string message, object data = null)
        {
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                CSharpCategory,
                FormatCategory(category)
            };

            var m = new StringBuilder();

            foreach (var c in categories)
            {
                m.Append(c);
                m.Append(" ");
            }

            m.Append(message?.Replace("'", "\\'") ?? string.Empty);

            var args = new List<object>(2)
            {
                // We always have a message
                m.Length > 0 ? m.ToString() : string.Empty
            };

            if (data != null) args.Add(data);

            var builder2 = new StringBuilder();
            builder2.Append("if (window && window.log) {");
            builder2.AppendFormat("  log('{0}'", args[0]);
            if (args.Count == 2)
            {
                builder2.Append(", ");
                builder2.Append(JsonConvert.SerializeObject(args[1],
                    JavaScriptBridgeMessageExtensions.MessageSerializationSettings));
            }

            builder2.AppendLine(");");
            builder2.Append("}");

            _webView.TryEvalAsync(builder2.ToString());
        }
    }
}