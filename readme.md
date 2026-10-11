# Call native code from JavaScript in the Win32 WebView (no AddWebAllowedObject)

The Win32 [WebViewControl](https://learn.microsoft.com/uwp/api/windows.web.ui.interop.webviewcontrol) (EdgeHTML) does not support `AddWebAllowedObject` (sometimes searched as `AddAllowedWebObject`) or `ObjectForScripting`, so you cannot inject a native object into the page. This sample shows a JavaScript bridge that uses `window.external.notify` and the [`ScriptNotify`](https://learn.microsoft.com/uwp/api/windows.web.ui.interop.webviewcontrol.scriptnotify) event, with promise-based calls, for WinForms and WPF.

If you are building a new app, use [WebView2](https://learn.microsoft.com/microsoft-edge/webview2/) instead. WebView2 supports [`AddHostObjectToScript`](https://learn.microsoft.com/microsoft-edge/webview2/how-to/hostobject). See [Migrating to WebView2](#migrating-to-webview2).

## Status

This sample targets a legacy control. Use it to maintain an existing app, not to start a new one.

- **The control is deprecated.** The Windows [Deprecated features](https://learn.microsoft.com/windows/whats-new/deprecated-features) page (checked 2026-10-11) lists Legacy Web View, announced September 2025: "These components, built on the EdgeHTML engine, are no longer in active development and are being phased out." The same entry says they are "expected to eventually stop receiving nonsecurity and security updates and will be removed from future versions of Windows."
- **The original package is deprecated.** NuGet marks `Microsoft.Toolkit.Win32.UI.Controls` 5.0.0 as deprecated (Legacy, CriticalBugs) with this message: "This package is now obsolete and contains no DLLs. Please use Microsoft.Toolkit.Wpf.UI.Controls.WebView or Microsoft.Toolkit.Forms.UI.Controls.WebView packages instead."
- **The replacement packages are deprecated too.** [Microsoft.Toolkit.Wpf.UI.Controls.WebView](https://www.nuget.org/packages/Microsoft.Toolkit.Wpf.UI.Controls.WebView) and [Microsoft.Toolkit.Forms.UI.Controls.WebView](https://www.nuget.org/packages/Microsoft.Toolkit.Forms.UI.Controls.WebView) 6.1.2 are the last versions. NuGet marks both as Legacy and names `Microsoft.Web.WebView2` as the alternate package: "The Toolkit Edge-HTML based WebView control has been replaced with the new Microsoft Edge (Chromium) based WebView2." Their `WebView` classes carry `[Obsolete]`, so the compiler reports warning CS0618 when you use them. `NavigateToLocal`, used below, also reports CS0618 and points to `NavigateToLocalStreamUri`.
- **The projects in this repository** still reference `Microsoft.Toolkit.Win32.UI.Controls` 4.0.1. The **JavaScriptBridge** source also compiles unchanged against `Microsoft.Toolkit.Wpf.UI.Controls.WebView` 6.1.2 and `Microsoft.Toolkit.Forms.UI.Controls.WebView` 6.1.2.

## How the bridge works

The bridge has two halves: [Bridge.js](JavaScriptBridge/Bridge.js) runs in the page, and [JavaScriptBridge.cs](JavaScriptBridge/JavaScriptBridge.cs) runs in your app.

1. `JavaScriptBridge.CreateAndStart` turns on JavaScript and `ScriptNotify` for the WebView. It also records the URIs that may talk to the bridge.
2. When the page raises `DOMContentLoaded` from an allowed URI, the bridge injects `Bridge.js` if the page does not already have it. `Bridge.js` creates `window.JavaScriptBridge` and fires a `JavaScriptBridgeReady` event.
3. Your page calls `JavaScriptBridge.callNative(handlerId, data)`. The call adds a `{ handler, handlerdata, callbackId }` message to a queue. Then it calls `window.external.notify("jsbridge://queue_message")`.
4. The `ScriptNotify` handler drops any event whose URI is not in `AllowedScriptNotifyUris`, or whose value is not a bridge message. Otherwise, it calls `JavaScriptBridge.fetchQueue()` through `InvokeScriptAsync` and gets every queued message.
5. For each message, the bridge runs the C# delegate you registered with `AddScriptingHandler`. If the delegate throws, or no handler has that name, the reply carries an error object instead. Its `message` is the exception text.
6. The bridge sends each reply back through `InvokeScriptAsync`, which runs `JavaScriptBridge.handleNativeMessage(...)`. That call resolves the promise (or runs the success callback) with the result. An error rejects the promise (or runs the error callback).

A minimal round trip looks like this. In C#:

```csharp
_javaScriptBridge = JavaScriptBridge.CreateAndStart(webView1, new Uri("ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html"));
_javaScriptBridge.AddScriptingHandler("HelloWorld", @params => $"Hello, {@params["name"]}!");
```

In the page, using the `ConnectWebViewBridge` helper defined in [Add the page content](#add-the-page-content):

```javascript
ConnectWebViewBridge(async function (bridge) {
    const response = await bridge.callNative("HelloWorld", { name: "World" });
    console.log(response); // "Hello, World!"
});
```

## Prerequisites

- Windows 10, version 1803 or later. The `WebViewControl` class was introduced in that version.
- Visual Studio with the .NET desktop development workload.
- .NET Framework 4.6.2 or later. The WebView packages require it.

## Getting started

Follow these steps to add the bridge to a new WinForms or WPF app. The examples show both frameworks, so use the one that matches your app.

### Create the project

1. Clone this repository or download the source code. You need the **JavaScriptBridge** folder.
1. Create a new WinForms or WPF project that targets .NET Framework 4.6.2 or later.
1. Install the WebView package for your UI framework, version 6.1.2:
   - WPF: `Microsoft.Toolkit.Wpf.UI.Controls.WebView`
   - WinForms: `Microsoft.Toolkit.Forms.UI.Controls.WebView`
1. Copy the **JavaScriptBridge** folder into your solution folder.
1. In **Solution Explorer**, right-click your solution, then select **Add** > **Existing Project**. Select `JavaScriptBridge.csproj`.
1. In the **JavaScriptBridge** project, replace the `Microsoft.Toolkit.Win32.UI.Controls` package with the same WebView package your app uses. Update `Newtonsoft.Json` to 13.0.1 or later.
1. In your app project, add a project reference to **JavaScriptBridge**.

The **JavaScriptBridge** project copies `Bridge.js` to the build output. The bridge reads the file by the relative path `Bridge.js`, so the working directory of your app must be the output folder. This is the default when you start the app from Visual Studio or from its own folder.

### Add a WebView

Add a `WebView` control to your window or form. The WPF control is `Microsoft.Toolkit.Wpf.UI.Controls.WebView`. The WinForms control is `Microsoft.Toolkit.Forms.UI.Controls.WebView`.

#### Configure the WebView in WPF

These examples name the control `WebView`. Add it to `MainWindow.xaml`:

```xml
<Window
    x:Class="AddWebAllowedObject_GettingStarted.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="clr-namespace:Microsoft.Toolkit.Wpf.UI.Controls;assembly=Microsoft.Toolkit.Wpf.UI.Controls.WebView"
    Title="MainWindow">
    <Grid>
        <controls:WebView x:Name="WebView" Loaded="WebView_OnLoaded" />
    </Grid>
</Window>
```

Then replace the contents of `MainWindow.xaml.cs`:

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

#### Configure the WebView in WinForms

These examples use the default designer name, `webView1`. Replace the contents of `Form1.cs`:

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

### Add the page content

The code in the previous step calls `NavigateToLocal("/Content.html")`. Create that page now.

1. Add a new HTML page named `Content.html` to your app project.
1. In the **Properties** window for `Content.html`, set **Copy to Output Directory** to **Copy if newer**.
1. Replace the contents of `Content.html` with the following markup.

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

The `script` block defines `ConnectWebViewBridge`. This helper runs your code as soon as the bridge is ready. It checks for `window.JavaScriptBridge` first, then waits for the `JavaScriptBridgeReady` event. The script sits at the end of the `body` tag so it does not block other scripts, and the DOM is ready before the script runs.

Run the app. You should see an empty window titled *JavaScript Bridge: Getting Started*.

### Find the page URI

The bridge only accepts messages from URIs that you allow. To allow your page, you need its exact URI. You can read it with a debugger.

The debugger for this control is [Microsoft Edge DevTools Preview](https://www.microsoft.com/store/productId/9MZBFRMZ0MNJ) from the Microsoft Store. The Windows deprecation entry above also lists the legacy Microsoft Edge (EdgeHTML) DevTools, so this tool may not stay available.

1. Install **Microsoft Edge DevTools Preview** from the Microsoft Store.
1. Start your app.
1. Start **Microsoft Edge DevTools Preview**.
1. Under **Local**, find the **Debug Target** titled **JavaScript Bridge: Getting Started**. Its URI is `ms-local-stream://microsoft.win32webviewhost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html`.
1. Select the target. A new window opens with the debugger attached to your page.
1. Select the **Console** tab.
1. Type `document.URL` and press **Enter**. The console shows `"ms-local-stream://Microsoft.Win32WebViewHost_cw5n1h2txyewy_4c6f63616c436f6e74656e74/Content.html"`. The host casing differs from the samples, which use lowercase. On current .NET, `System.Uri` comparison ignores host case, so both forms match. If the bridge does not load, pass the exact `document.URL` value to `CreateAndStart`.
1. Copy the value, then close the debugger and your app.

### Start the bridge

To start the bridge, you need two things: the `IWebView` instance you added earlier, and the page URI from the previous step. The bridge listens for [`IWebView.ScriptNotify`](https://learn.microsoft.com/uwp/api/windows.web.ui.interop.webviewcontrol.scriptnotify) events from that URI only.

#### Start the bridge in WPF

Replace the contents of `MainWindow.xaml.cs`:

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

#### Start the bridge in WinForms

Replace the contents of `Form1.cs`:

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

To check that the bridge loaded:

1. Start your app.
1. Start **Microsoft Edge DevTools Preview** and attach to the **JavaScript Bridge: Getting Started** target, as in [Find the page URI](#find-the-page-uri).
1. Select the **Console** tab.

The console shows `JavaScript bridge is ready!`. If it does not, see [Troubleshooting](#troubleshooting).

### Call .NET methods from JavaScript

A scripting handler connects a name to a .NET delegate. The delegate takes named parameters and can return a value. Register a handler with `JavaScriptBridge.AddScriptingHandler`.

#### Add a scripting handler in WPF

Replace the contents of `MainWindow.xaml.cs`:

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

#### Add a scripting handler in WinForms

Replace the contents of `Form1.cs`:

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

This code registers a handler named `HelloWorld`. The lambda takes the named parameters as `@params`, a `Dictionary<string, object>`.

### Call the HelloWorld handler

To call `HelloWorld`, the page calls `callNative` on the bridge. In this example, the **Hello, World!** button calls it.

`callNative` takes a handler ID, optional data for the handler as named arguments, an optional success callback, and an optional error callback. You can call it three ways: with callbacks, with a promise, or with `async` and `await`. Pick the style that matches your code. In each example below, replace the `ConnectWebViewBridge(function (bridge) { ... });` call at the end of the `script` block in `Content.html`. Keep the helper definition above it.

#### Use a callback

This example passes the handler ID and a success callback.

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

#### Use a promise

Without callbacks, `callNative` returns a `Promise`. This example uses `.then` to handle the result.

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

#### Use async and await

This example awaits the `Promise` that `callNative` returns.

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

Run your app and select **Hello, World!**. The text `Hello, World!` appears below the button.

### Pass parameters to .NET methods

The `HelloWorld` handler takes no parameters and returns a string. Next, change it to take a `name` parameter and include it in the response.

In your .NET code, change the handler from this:

```csharp
_javaScriptBridge.AddScriptingHandler(
    "HelloWorld",
    @params => "Hello, World!");
```

To this:

```csharp
_javaScriptBridge.AddScriptingHandler(
    "HelloWorld",
    @params => $"Hello, {@params["name"]}!");
```

In the page, pass the data as the second argument to `callNative`, after the handler ID. The data is a JavaScript object of key and value pairs:

```javascript
const handlerData = { name: "World" };
```

This object has one property, `name`, with the string value `World`. The bridge turns it into the `Dictionary<string, object>` that the handler receives as `@params`.

#### Pass parameters with a callback

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

#### Pass parameters with a promise

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

#### Pass parameters with async and await

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

Run your app and select **Hello, World!**. The text `Hello, World!` appears below the button. If you change `World` to another name, the text changes to match.

### Handle exceptions

When a .NET handler throws an exception, the bridge returns an object that describes the exception. With a callback, the bridge passes it to your error callback. With a promise, the bridge rejects the promise. With `await`, your code can catch it.

In your .NET code, change the handler from this:

```csharp
_javaScriptBridge.AddScriptingHandler(
    "HelloWorld",
    @params => $"Hello, {@params["name"]}!");
```

To this:

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

Check every parameter that comes from JavaScript, as this handler does.

Then add error handling to the page. Use the example that matches your style.

#### Handle exceptions with a callback

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

#### Handle exceptions with a promise

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

#### Handle exceptions with async and await

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

Run your app. The output is `Hello, World!`, as before. To see the error handler run, change the data in the page from this:

```javascript
const handlerData = { name: "World" };
```

To this:

```javascript
const handlerData = { name2: "World" };
```

The handler expects a key named `name`. When you select the button, the page shows this error:

```json
{
  "message": "Expected parameter 'name'.\\r\\nParameter name: params",
  "data": {},
  "source": "JavaScript Bridge",
  "hResult": -2146233088
}
```

### Return complex objects

The bridge can also return .NET objects, as long as they can be serialized to JSON. Update the handler in your .NET code:

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

The handler checks the number of parameters, as before. Then it builds an `AddressBookEntry` from the keys in the dictionary. Add the `AddressBookEntry` class to your project so the code compiles:

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

If you run the app without changing the page, the bridge returns this error:

```json
{
  "message": "Parameters did not produce a valid name.",
  "data": {},
  "source": "JavaScript Bridge",
  "hResult": -2146233088
}
```

The page sent one parameter, but the handler cannot use its key to build an `AddressBookEntry`. So the handler throws an `InvalidOperationException` instead of returning an empty entry.

To fix this, send the keys the handler expects. As before, the data is a JavaScript object of key and value pairs:

```javascript
const handlerData = {
                firstName: "Hello",
                lastName: "World"
        };
```

#### Send multiple parameters with a callback

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

#### Send multiple parameters with a promise

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

#### Send multiple parameters with async and await

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

Run your app and select **Hello, World!**. The page shows the serialized `AddressBookEntry`:

```json
{
  "firstName": "Hello",
  "lastName": "World",
  "fullName": "Hello World"
}
```

## Troubleshooting

The bridge writes trace messages with the category `JavaScript Bridge`. When you debug in Visual Studio, they appear in the **Output** window.

- **The console never shows `JavaScript bridge is ready!`.** Check that the URI you passed to `CreateAndStart` matches `document.URL`. The bridge injects `Bridge.js` only for allowed URIs. Also check that `Bridge.js` is in the working directory of your app. The bridge loads the file without awaiting the result, so a missing file does not show an error in your app. To spot this case, look for the `Could not detect JavaScriptBridge. Injecting script` trace followed by a `FileNotFoundException` from the debugger in the Output window.
- **The Output window shows `ScriptNotify received from unknown origin`.** The page URI is not in the allowed list. Pass the exact URI to `CreateAndStart`.
- **The error message is `Handler '<name>' not supported.`** No handler with that name is registered. Check the name you passed to `AddScriptingHandler`. Names are not case sensitive.

## Migrating to WebView2

WebView2 has two built-in ways to call native code from JavaScript, so you do not need this bridge.

| This bridge (WebViewControl) | WebView2 |
| --- | --- |
| `window.external.notify(message)` | `window.chrome.webview.postMessage(message)` |
| `ScriptNotify` event | `CoreWebView2.WebMessageReceived` event |
| `InvokeScriptAsync` to send a reply | `CoreWebView2.PostWebMessageAsString` or `PostWebMessageAsJson` |
| `AddScriptingHandler` plus `callNative` | `CoreWebView2.AddHostObjectToScript` plus `chrome.webview.hostObjects` |

### Use web messages

Add the `Microsoft.Web.WebView2` package and a `WebView2` control named `webView`. Then, in an `async` method of your window or form:

```csharp
await webView.EnsureCoreWebView2Async();
webView.CoreWebView2.WebMessageReceived += (sender, e) =>
    webView.CoreWebView2.PostWebMessageAsString($"Hello, {e.TryGetWebMessageAsString()}!");
```

In the page:

```javascript
window.chrome.webview.addEventListener("message", e => console.log(e.data));
window.chrome.webview.postMessage("World");
```

### Use a host object

First, define a COM visible class:

```csharp
using System.Runtime.InteropServices;

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class Greeter
{
    public string Hello(string name) => $"Hello, {name}!";
}
```

Then add an instance to the page:

```csharp
// After EnsureCoreWebView2Async completes:
webView.CoreWebView2.AddHostObjectToScript("greeter", new Greeter());
```

In the page, call the method from an `async` function:

```javascript
async function greet() {
    const text = await chrome.webview.hostObjects.greeter.Hello("World");
    console.log(text); // "Hello, World!"
}
```

### Learn more about WebView2

- [Interop of native-side and web-side code](https://learn.microsoft.com/microsoft-edge/webview2/how-to/communicate-btwn-web-native)
- [Call native-side code from web-side code](https://learn.microsoft.com/microsoft-edge/webview2/how-to/hostobject)
- [CoreWebView2.AddHostObjectToScript method](https://learn.microsoft.com/dotnet/api/microsoft.web.webview2.core.corewebview2.addhostobjecttoscript)

## FAQ

### Why is AddWebAllowedObject missing in the Win32 WebView?

[`AddWebAllowedObject`](https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.webview.addweballowedobject) belongs to the UWP `Windows.UI.Xaml.Controls.WebView` class. The Win32 [`WebViewControl`](https://learn.microsoft.com/uwp/api/windows.web.ui.interop.webviewcontrol) class does not have it, and neither do the WinForms and WPF wrappers built on that class.

### How do I call C# from JavaScript in WebViewControl?

Call `window.external.notify` from the page and handle the `ScriptNotify` event in your app. This bridge does that for you. It adds a message queue, named handlers, and promise, callback, and async/await support.

### Should I use this for a new app?

No. The control and its packages are deprecated. Use WebView2.

### What is the WebView2 equivalent?

Use `CoreWebView2.AddHostObjectToScript` to expose a native object. Or use `window.chrome.webview.postMessage` with the `WebMessageReceived` event to pass messages. See [Migrating to WebView2](#migrating-to-webview2).
