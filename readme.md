# Getting Started

The Win32 [WebViewControl](https://docs.microsoft.com/uwp/api/windows.web.ui.interop.webviewcontrol) does not support `ObjectForScripting` or `AddWebAllowedObject`. This sample was designed with the goal of showcasing how communications could be standardized using `window.external.notify` and the [`ScriptNotify`](https://docs.microsoft.com/uwp/api/windows.web.ui.interop.webviewcontrol.scriptnotify) event.

## Clone

Clone this repository or download the source code. You will need the contents of **JavaScriptBridge** for this sample.

## Create new project

- Create a new WinForms or WPF project targeting **.NET Framework 4.6.2** or later. Samples are given for both WinForms and WPF, and .NET 4.6.2 is required for the WebView NuGet package.
- Install **Microsoft.Toolkit.Win32.UI.Controls** NuGet package to project.
- Copy **JavaScriptBridge** project from clone to your solution
- Use **Add existing project** to add the copied **JavaScriptBridge** project to your new solution.
- From your WinForms or WPF project, use **Add reference** to add the **JavaScriptBridge** project.

## Add a WebView

When using Visual Studio 15.8, the WebView control shows up automatically in your toolbox. If using an older version of Visual Studio, use the instructions in the documentation to [Add the WebView control to the Visual Studio Toolbox](https://docs.microsoft.com/en-us/windows/communitytoolkit/controls/webview#add-the-webview-control-to-the-visual-studio-toolbox).

### Basic Configuration For WPF

For WPF we assume a WebView has been added with the name *WebView*.

**MainWindow.xaml.cs**

```csharp
using System.Windows;

namespace AddWebAllowedObject_GettingStarted
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

             WebView.DOMContentLoaded += (o, e) => Title = WebView.DocumentTitle;
        }

        private void WebView_OnLoaded(object sender, RoutedEventArgs e)
        {
            WebView.NavigateToLocal("/Content.html");
        }
    }
}
```

### Basic Configuration For WinForms

For WinForms we assume a WebView has been added via the designer with the default name *webView1*.

**Form1.cs**

```csharp
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            webView1.DOMContentLoaded += (o, e) => Text = webView1.DocumentTitle;

            webView1.NavigateToLocal("/Content.html");
        }
    }
}
```

## Add Local Content

In the previous step we use the method `NavigateToLocal(String)` method to navigate to content. To create that content:

- Add a new HTML page to your project called **Content.html**.
- After adding, ensure the property **Copy to Output Directory** is set to **Copy if newer**.
- Replace the default content with the following

**Content.html**

```html
<!DOCTYPE html>

<html lang="en" xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta charset="utf-8" />
    <title>JavaScript Bridge: Getting Started</title>
</head>
<body>
    <button id="hello-world">Hello, World!</button>
    <div id="output"></div>

    <script>
        var ConnectWebViewBridge = function (callback) {
            if (window.JavaScriptBridge) {
                callback(JavaScriptBridge);
            } else {
                // Bridge is not yet loaded, wait for event
                document.addEventListener(
                    "JavaScriptBridgeReady",
                    function () {
                        callback(JavaScriptBridge);
                    },
                    false);
            }
        };

        ConnectWebViewBridge(function (bridge) {
            console.log("JavaScript bridge is ready!");
        });
    </script>
</body>
</html>
```

The content includes basic HTML elements, and the `script` block used to wait for the JavaScript bridge to initialize. You can place it wherever you would like, but it is placed in this sample at the end of the `body` tag so as not to block any other scripts, and the DOM has the ability to get ready before parsing and invoking our script.

If you run the sample at this point, there should be an empty window with a title of *JavaScript Bridge Getting Started*.

## Debugging your WebView

During the course of development you may wish to debug the content in the WebView instance. To do that, you will need to use the [Microsoft Edge DevTools Preview](https://www.microsoft.com/store/productId/9MZBFRMZ0MNJ), available from the Microsoft Store.

- Install [Microsoft Edge DevTools Preview](https://www.microsoft.com/store/productId/9MZBFRMZ0MNJ) from the Microsoft Store.
- Launch your application.
- Launch **Microsoft Edge DevTools Preview**
- Look for a **Local** **Debug Target** with the title **JavaScript Bridge Getting Started** with the URI **ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html**
- Click that item to attach the debugger. Once attached a new window will pop up to debug your local content within the WebView.

Once the debugger is attached, we will need the URI to configure the JavaScript bridge. You can easily get the URI of the document by following these steps

- With the Microsoft Edge DevTools Preview attached to your WebView instance, click on the **Console** tab.
- Next to the `>` symbol, type `document.URL` which will output `"ms-local-stream://Microsoft.Win32WebViewHost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html"`
- Copy the returned value. We'll use that in the configuration step.
- After the value has been copied, close the debugger and your application instance.

## Inject the JavaScript bridge into your content

Injecting the JavaScript bridge into your content requires two items: an instance of `IWebView`, which we have added in a previous step, and the expected location of content that we will listen to [`IWebView.ScriptNotify`](https://docs.microsoft.com/uwp/api/windows.web.ui.interop.webviewcontrol.scriptnotify) events, which we just received from the debugger.

To configure the JavaScript bridge, return to your .NET code-behind and replace the contents with the following.

### Adding JavaScript Bridge for WPF

**MainWindow.xaml.cs**

```csharp
using System;
using System.Windows;
using Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions;

namespace AddWebAllowedObject_GettingStarted
{
    public partial class MainWindow : Window
    {
        private JavaScriptBridge _javaScriptBridge;

        public MainWindow()
        {
            InitializeComponent();

            _javaScriptBridge = JavaScriptBridge.CreateAndStart(
                WebView,
                new Uri("ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html"));

            WebView.DOMContentLoaded += (o, e) => Title = WebView.DocumentTitle;
        }

        private void WebView_OnLoaded(object sender, RoutedEventArgs e)
        {
            WebView.NavigateToLocal("/Content.html");
        }
    }
}
```

### Adding JavaScript Bridge for WinForms

**Form1.cs**

```csharp
using Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions;
using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private JavaScriptBridge _javaScriptBridge;

        public Form1()
        {
            InitializeComponent();

            _javaScriptBridge = JavaScriptBridge.CreateAndStart(
                webView1,
                new Uri("ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html"));


            webView1.DOMContentLoaded += (o, e) => Text = webView1.DocumentTitle;

            webView1.NavigateToLocal("/Content.html");
        }
    }
}
```

After initialization, let's check to make sure everything is working by debugging the WebView:

- Launch your application
- Launch **[Microsoft Edge DevTools Preview](https://www.microsoft.com/store/productId/9MZBFRMZ0MNJ)**
- Look for a **Local** **Debug Target** with the title **JavaScript Bridge Getting Started** with the URI **ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html**
- Click that item to attach the debugger. Once attached a new window will pop up to debug your local content within the WebView.
- Click on the **Console** tab of the debugger.
- There should be a message `JavaScript bridge is ready!`

If you see the ready message, the JavaScript bridge was successfully injected into the running WebView instance.

## Calling .NET methods from JavaScript

Now that everything is configured and the JavaScript bridge is injected into your content page, we can begin using it by configuring a scripting handler. A script handler takes two pieces of information, an identifier, and a delegate to execute with named parameters and an optional return value. To register a script handler from our .NET code, use the `JavaScriptBridge.AddScriptingHandler` method.

### Add Scripting Handler for WPF

**MainWindow.xaml.cs**

```csharp
using System;
using System.Windows;
using Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions;

namespace AddWebAllowedObject_GettingStarted
{
    public partial class MainWindow : Window
    {
        private JavaScriptBridge _javaScriptBridge;

        public MainWindow()
        {
            InitializeComponent();

            _javaScriptBridge = JavaScriptBridge.CreateAndStart(
                WebView, 
                new Uri("ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html"));
            _javaScriptBridge.AddScriptingHandler(
                "HelloWorld", 
                @params => "Hello, World!");

            WebView.DOMContentLoaded += (o, e) => Title = WebView.DocumentTitle;
        }

        private void WebView_OnLoaded(object sender, RoutedEventArgs e)
        {
            WebView.NavigateToLocal("/Content.html");
        }
    }
}
```

### Add Scripting Handler for WinForms

**Form1.cs**

```csharp
using Microsoft.Toolkit.Win32.UI.Controls.WebViewExtensions;
using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private JavaScriptBridge _javaScriptBridge;

        public Form1()
        {
            InitializeComponent();

            _javaScriptBridge = JavaScriptBridge.CreateAndStart(
                webView1,
                new Uri("ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html"));
            _javaScriptBridge.AddScriptingHandler(
                "HelloWorld",
                @params => "Hello, World!");

            webView1.DOMContentLoaded += (o, e) => Text = webView1.DocumentTitle;

            webView1.NavigateToLocal("/Content.html");
        }
    }
}
```

A scripting handler is registered with the id *HelloWorld*, which then specifies a lambda taking the named parameters called *@params*, which is a `Dictionary<string, object>` instance.

## Calling HelloWorld scripting handler

To invoke our registered scripting handler *HelloWorld*, we need to invoke the `callNative` method on the JavaScript bridge injected into our page. For our sample we will handle the click event of the **Hello, World!** button and call the JavaScript bridge.

The signature for `callNative` accepts a handlerId, data to pass that handler in the form of named arguments, a success callback function, and an error callback function. The bridge accepts three types of invocations: callbacks, JavaScript Promises, and async/await syntax.  Each implementation is shown in that order and you can pick the one that matches your coding style.

### Hello World using JavaScript Callbacks

This example only passes in the handlerId and a success function.

**Content.html**

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        bridge.callNative(handlerId, function(response) {
            const log = document.getElementById("output");
            log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
        });
    };
});
```

### Hello World using JavaScript Promise

The JavaScript bridge also supports Promises. As before, we invoke `callNative`, which returns a `Promise` (the eventual result) instead of our value.

This example passes in the handlerId as before, and uses `.then` to handle success.

**Content.html**

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        bridge.callNative(handlerId)
              .then(function (response) {
                        const log = document.getElementById("output");
                        log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;});
    };
});
```

### Hello World using JavaScript Async/Await

The JavaScript bridge also supports the async/await syntax. As before, we invoke `callNative`, which returns a `Promise`, that we then `await`.

This example passes in the handlerId as before, awaits the response, then outputs to the log as before.

**Content.html**

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = async function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const response = await bridge.callNative(handlerId);
        const log = document.getElementById("output");
        log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
    };
});
```

Run your project. After clicking the *Hello, World!* button, you should see the text `Hello, World!` below the button.

## Calling .NET methods with parameters

So far we have used the `HelloWorld` script handler, which takes no parameters and returns a string. Let's now refactor that delegate to take a single parameter, `name`, and include that in the response.

Let's go back to our .NET code and change the scripting handler from

```csharp
_javaScriptBridge.AddScriptingHandler(
    "HelloWorld",
    @params => "Hello, World!");
```

to the following

```csharp
_javaScriptBridge.AddScriptingHandler(
    "HelloWorld",
    @params => $"Hello, {@params["name"]}!");
```

We also need to change the JavaScript to pass in the data. `callNative` includes a parameter for passing the handler data, after passing the handler id. The data is expected to be passed as a set of key/value pairs as a JavaScript object.

```javascript
const handlerData = { name: "World" };
```

The data must be passed as a set of key/value pairs. In the above example, a new JavaScript object with the property `name` is created, with a string value of *World*. This is deserialized by the bridge into a `Dictionary<string,object>` that is passed in as the *@params* parameter.

Here is what the inclusion of the data parameter looks like in the various syntaxes in JavaScript.

### Hello World with Parameters using JavaScript Callback

**Content.html**

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
        helloWorldButton.onclick = function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const handlerData = { name: "World" };
        bridge.callNative(handlerId, handlerData, function(response) {
            const log = document.getElementById("output");
            log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
        });
    };
});
```

### Hello World with Parameters using JavaScript Promise

**Content.html**

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const handlerData = { name: "World" };
        bridge.callNative(handlerId, handlerData)
              .then(function (response) {
                     const log = document.getElementById("output");
                    log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
                });
    };
});
```

### Hello World with Parameters using JavaScript Async/Await

**Content.html**

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = async function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const handlerData = { name: "World" };
        const response = await bridge.callNative(handlerId, handlerData);
        const log = document.getElementById("output");
        log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
        };
    });
```

If you run the project you'll see the familiar `Hello, World!` text below the button. If you change the handler data to something else (say, your name), you would expect to see something like `Hello, Richard!` below the button.

## Exception Handling

It may be the case that when calling .NET code an exception is encountered. This is handled automatically by the JavaScript bridge and is raised as either a callback, JavaScript Promise reject, or an exception that can be caught with async/await. In each case an object representing the .NET exception is returned via the bridge.

Let's go back to our .NET code and change the scripting handler from

```csharp
_javaScriptBridge.AddScriptingHandler(
    "HelloWorld",
    @params => $"Hello, {@params["name"]}!");
```

to the following

```csharp
_javaScriptBridge.AddScriptingHandler("HelloWorld", @params =>
{
    if (@params == null)
    {
        throw new ArgumentNullException(nameof(@params));
    }

    if (@params.Count != 1)
    {
        throw new ArgumentOutOfRangeException(nameof(@params), "Expected one parameter.");
    }

    if (!@params.ContainsKey("name"))
    {
        throw new ArgumentOutOfRangeException(nameof(@params), "Expected parameter 'name'.");
    }

    return $"Hello, {@params["name"]}!";
});
```

>When implementing this in your project it is always a good idea to check the parameters coming from JavaScript.

To handle the error events, the JavaScript code needs to be altered to provide an error callback or catch the exception from the `Promise`. Examples are given below for each syntax.

### Handling Exceptions using JavaScript Callback

**Content.html**

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const handlerData = { name: "World" };

        bridge.callNative(
                    handlerId,
                    handlerData,
                    function(response) {
                        const log = document.getElementById("output");
                        log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
                        console.info(response);
                    }, function(err) {
                        const log = document.getElementById("output");
                        log.innerHTML = `<pre><code>${JSON.stringify(err, null, 2)}</code></pre>`;
                        console.error(err.message);
                    });
    };
});
```

### Handling Exceptions using JavaScript Promise

**Content.html**

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const handlerData = { name: "World" };

        bridge.callNative(handlerId, handlerData)
              .then(function(response) {
                        const log = document.getElementById("output");
                        log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
                        console.info(response);})
               .catch(function(err) {
                        const log = document.getElementById("output");
                        log.innerHTML = `<pre><code>${JSON.stringify(err, null, 2)}</code></pre>`;
                        console.error(err.message);});
    };
});
```

### JavaScript Async/Await

**Content.html**

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = async function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const handlerData = { name: "World" };
        try {
            const response = await bridge.callNative(handlerId, handlerData);
            const log = document.getElementById("output");
            log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
            console.info(response);
        } catch (err) {
            const log = document.getElementById("output");
            log.innerHTML = `<pre><code>${JSON.stringify(err, null, 2)}</code></pre>`;
            console.error(err.message);
        }
    };
});
```

If you run the project the output should appear as before: `Hello, World!`. To see the error handler in action, change the JavaScript code from

```javascript
const handlerData = { name: "World" };
```

to

```javascript
const handlerData = { name2: "World" };
```

Since the .NET script handler is expecting a dictionary key the name "*name*", after clicking the button you should see the following on the screen

```json
{
  "message": "Expected parameter 'name'\\r\\nParameter name: params",
  "data": {},
  "source": "JavaScript Bridge",
  "hResult": -2146233088
}
```

## Returning Complex Objects

The JavaScript bridge is also capable of returning .NET objects and structures, so long as they can be serialized to JSON. Let's return to our .NET code and update the scripting handler.

```csharp
_javaScriptBridge.AddScriptingHandler("HelloWorld", @params =>
{
    if (@params == null)
    {
        throw new ArgumentNullException(nameof(@params));
    }

    if (@params.Count == 0 || @params.Count > 3)
    {
        throw new ArgumentOutOfRangeException(nameof(@params), "Expected three or less parameters");
    }

    var retval = new AddressBookEntry();

    if (@params.ContainsKey("firstName"))
    {
        retval.FirstName = @params["firstName"]?.ToString();
    }

    if (@params.ContainsKey("lastName"))
    {
        retval.LastName = @params["lastName"]?.ToString();
    }

    if (@params.ContainsKey("company"))
    {
        retval.Company = @params["company"]?.ToString();
    }

    if (string.IsNullOrEmpty(retval.FullName))
    {
        throw new InvalidOperationException("Parameters did not produce a valid name.");
    }

    return retval;
});
```

The handler includes checks on the number of parameters as before, and builds up a .NET type `AddressBookEntry` based on the keys of the dictionary passed in. To compile the sample, we'll also need to add the `AddressBookEntry` class to the project.

```csharp
public class AddressBookEntry
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Company { get; set; }
    public string FullName
    {
        get
        {
            var fnEmpty = string.IsNullOrEmpty(FirstName);
            var lnEmpty = string.IsNullOrEmpty(LastName);
            var cEmpty = string.IsNullOrEmpty(Company);

            if (fnEmpty && lnEmpty && !cEmpty)
            {
                return Company;
            }

            if (!fnEmpty || !lnEmpty)
            {
                var sb = new StringBuilder(256);
                if (!fnEmpty)
                {
                    sb.Append(FirstName);
                    sb.Append(" ");
                }

                if (!lnEmpty)
                {
                    sb.Append(LastName);
                    sb.Append(" ");
                }

                if (!cEmpty)
                {
                    sb.Append("(");
                    sb.Append(Company);
                    sb.Append(")");
                }

                return sb.ToString().Trim();
            }

            return string.Empty;
        }
    }

    public override string ToString()
    {
        return FullName;
    }
}
```

If we run the sample without changing the JavaScript, the following error is returned

```json
{
  "message": "Parameters did not produce a valid name.",
  "data": {},
  "source": "JavaScript Bridge",
  "hResult": -2146233088
}
```

The new delegate function expects some additional parameters to be sent in order to build the `AddressBookEntry`. Since one parameter was sent, the dictionary had one key, but it was not a key that was usable by the delegate. As such, it returned a `InvalidOperationException` instead of an empty instance of `AddressBookEntry`.

To take advantage of the new `AddressBookEntry` object, updates need to be made to the JavaScript. As before, data is passed in as key/value pairs.

```javascript
const handlerData = {
                firstName: "Hello",
                lastName: "World"
        };
```

### Multiple Parameters using JavaScript Callback

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const handlerData = {
                firstName: "Hello",
                lastName: "World"
        };

        bridge.callNative(
                handlerId,
                handlerData,
                function(response) {
                    const log = document.getElementById("output");
                    log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
                    console.info(response);
                }, function(err) {
                    const log = document.getElementById("output");
                    log.innerHTML = `<pre><code>${JSON.stringify(err, null, 2)}</code></pre>`;
                    console.error(err.message);
                });
    };
});
```

### Multiple Parameters using JavaScript Promise

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const handlerData = {
                firstName: "Hello",
                lastName: "World"
        };

        bridge.callNative(handlerId, handlerData)
              .then(function(response) {
                        const log = document.getElementById("output");
                        log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
                        console.info(response);
                })
                .catch(function (err) {
                        const log = document.getElementById("output");
                        log.innerHTML = `<pre><code>${JSON.stringify(err, null, 2)}</code></pre>`;
                        console.error(err.message);
                });
    };
});
```

### Multiple Parameters using JavaScript Async/Await

```javascript
ConnectWebViewBridge(function(bridge) {
    console.log("JavaScript bridge is ready!");

    const helloWorldButton = document.getElementById("hello-world");
    helloWorldButton.onclick = async function(e) {
        e.preventDefault();

        const handlerId = "HelloWorld";
        const handlerData = {
                firstName: "Hello",
                lastName: "World"
        };

        try {
            const response = await bridge.callNative(handlerId, handlerData);
            const log = document.getElementById("output");
            log.innerHTML = `<pre><code>${JSON.stringify(response, null, 2)}</code></pre>`;
            console.info(response);
        } catch (err) {
            const log = document.getElementById("output");
            log.innerHTML = `<pre><code>${JSON.stringify(err, null, 2)}</code></pre>`;
            console.error(err.message);
        }
    };
});
```

Running the project will a JSON response of the serialized `AddressBookEntry` class.

```json
{
  "firstName": "Hello",
  "lastName": "World",
  "fullName": "Hello World"
}
```