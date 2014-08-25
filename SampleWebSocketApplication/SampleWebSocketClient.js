var ws;
var logDiv;
var sendButton;
var messageInput;

function start() {
    logDiv = $('#log');
    sendButton = $('#send');
    messageInput = $('#message');

    if ("WebSocket" in window) {
        logDiv.append("Opening WebSocket connection<br>");
        ws = new WebSocket("ws://" + location.host + "/socket");
        ws.onopen = function () {
            logDiv.append("WebSocket connected<br>");
            messageInput.removeAttr('disabled');
            sendButton.removeAttr('disabled');
        };
        ws.onmessage = function (evt) {
            var receivedMsg = evt.data;
            logDiv.append("Received message: " + receivedMsg + "</br>");
        };
        ws.onclose = function () {
            logDiv.append("Websocket closed<br>");
            messageInput.attr("disabled", true);
            sendButton.attr("disabled", true);
        };
    }
    else {
        alert("WebSockets NOT supported by your Browser!");
    }
}

function sendMessage() {
    var messageText = messageInput.val();
    messageInput.val('');
    logDiv.append("Sending message: " + messageText + "<br>");
    ws.send(messageText);
}
