(function() {
    if (window.JavaScriptBridge) {
        return;
    }

    var sendMessageQueue = [],
        responseCallbacks = {},
        errorCallbacks = {},
        messagingFrame,
        msgCnt = 1;

    function handleNativeMessage(message) {
            dispatchNativeMessage(message);
    }

    function dispatchNativeMessage(messageJSON) {
        setTimeout(function _timeout() {
            const message = JSON.parse(jsonEscape(messageJSON));
            var responseCallback, errorCallback;

            // Check if some type of callback was specified
            if (message.callbackId) {
                // If there was an exception there will be data in message.errorData
                // We either have a callback for resolve or reject
                // Either way, we need to delete the appropriate callbacks

                if (message.errorData) {
                    // We have an Exception, delete the resolve callback and invoke the reject
                    responseCallback = responseCallbacks[message.callbackId];
                    if (responseCallback) {
                        delete responseCallbacks[message.callbackId];
                    }

                    errorCallback = errorCallbacks[message.callbackId];
                    if (errorCallback) {
                        errorCallback(message.errorData);
                        delete errorCallbacks[message.callbackId];
                    }
                } else {
                    errorCallback = errorCallbacks[message.callbackId];
                    if (errorCallback) {
                        delete errorCallbacks[message.callbackId];
                    }

                    responseCallback = responseCallbacks[message.callbackId];
                    if (responseCallback) {
                        responseCallback(message.responseData);
                        delete responseCallbacks[message.callbackId];
                    }
                }
            }
        });
    }

    function fetchQueue() {
        const queue = sendMessageQueue;
        sendMessageQueue = [];
        return queue;
    }

    function send(data, onSuccess, onFailure) {
        callNative(null, data, onSuccess, onFailure);
    }

    function callNative(handler, data, onSuccess, onFailure) {
        const func = this.callNative;

        data = data || {};
        if (typeof data === "function") {
            // No data specified. Need to shift params over by one
            onFailure = onSuccess;
            onSuccess = data;
            data = null;
        }

        if (onSuccess === undefined && onFailure === undefined) {
            return new Promise(function(resolve, reject) {
                func(handler,
                    data,
                    function(result) {
                        resolve(result);
                    },
                    function(err) {
                        reject(err);
                    });
            });
        }

        return sendImpl(
            { handler: handler, handlerdata: data },
            onSuccess,
            onFailure);
    }

    function sendImpl(message, onSuccess, onFailure) {
        if (onSuccess !== undefined || onFailure !== undefined) {
            const cbid = `cb_${msgCnt++}_${new Date().getTime()}`;

            message["callbackId"] = cbid;

            if (onSuccess) {
                responseCallbacks[cbid] = onSuccess;
            }

            if (onFailure) {
                errorCallbacks[cbid] = onFailure;
            }
        }

        sendMessageQueue.push(message);
        const notifyMessage = "jsbridge://queue_message";

        if (messagingFrame) {
            messagingFrame.contentWindow.postMessage(notifyMessage, "*");
        } else {
            window.external.notify(notifyMessage);
        }
    }

    function jsonEscape(input) {
        return input
            .replace(/\n/g, "\\\\n")
            .replace(/\r/g, "\\\\r")
            .replace(/\t/g, "\\\\t");
    }

    window.JavaScriptBridge = {
        send: send,
        callNative: callNative,
        fetchQueue: fetchQueue,
        handleNativeMessage: handleNativeMessage
    };

    // iframe to handle window.external.notify
    // TODO: Detect if this is necessary for your scenario
    //messagingFrame = document.createElement('iframe');
    //messagingFrame.style.display = 'none';
    //messagingFrame.src = "/Bridge.html";
    //document.documentElement.appendChild(messagingFrame);

    const readyEvent = new CustomEvent("JavaScriptBridgeReady");
    readyEvent.bridge = JavaScriptBridge;
    document.dispatchEvent(readyEvent);
})();