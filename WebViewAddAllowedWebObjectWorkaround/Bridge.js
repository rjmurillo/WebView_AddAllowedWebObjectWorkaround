;
(function() {
    if (window.JavaScriptBridge) {
        return;
    }

    var sendMessageQueue = [],
        responseCallbacks = {},
        messagingFrame,
        msgCnt = 1;

    function handleNativeMessage(message) {
            dispatchNativeMessage(message);
    }

    function dispatchNativeMessage(messageJSON) {
        setTimeout(function _timeout() {
            var message = JSON.parse(messageJSON);
            var responseCallback;

            if (message.callback) {
                responseCallback = responseCallbacks[message.callback];
                if (responseCallback) {
                    responseCallback(message.response);
                    delete responseCallbacks[message.callback];
                }
            }
        });
    }

    function fetchQueue() {
        var messageQueueString = JSON.stringify(sendMessageQueue);
        sendMessageQueue = [];
        return messageQueueString;
    }

    function send(data, callback) {
        sendImpl({ data: data }, callback);
    }

    function callNative(handler, data, callback) {
        sendImpl({ handler: handler, data: data }, callback);
    }

    function sendImpl(message, callback) {
        if (callback) {
            var cbid = 'cb_' + (msgCnt++) + '_' + new Date().getTime();
            responseCallbacks[cbid] = callback;
            message['callback'] = cbid;
        }

        sendMessageQueue.push(message);
        var notifyMessage = "jsbridge://queue_message";

        if (messagingFrame) {
            messagingFrame.contentWindow.postMessage(notifyMessage, "*");
        } else {
            window.external.notify(notifyMessage);
        }
    };

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

    var readyEvent = new CustomEvent('JavaScriptBridgeReady');
    readyEvent.bridge = JavaScriptBridge;
    document.dispatchEvent(readyEvent);
})();