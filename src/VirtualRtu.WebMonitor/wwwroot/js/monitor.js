var connection = new signalR.HubConnectionBuilder().withUrl("/monitorHub").build();

connection.on("ReceiveMessage",
    function(id, message) {
        var root = document.getElementById(id);
        writeLine(root, message);
    });

connection.start();

$.connection.hub.connectionSlow(function() {
    notifyUserOfConnectionProblem();
});

$.connection.hub.reconnecting(function() {
    notifyUserOfTryingToReconnect();
});

$.connection.hub.disconnected(function() {
    if (tryingToReconnect) {
        notifyUserOfDisconnect(); // Your function to notify user.
    }
});

var tryingToReconnect = false;

$.connection.hub.reconnecting(function() {
    tryingToReconnect = true;
});

$.connection.hub.reconnected(function() {
    tryingToReconnect = false;
});


function notifyUserOfConnectionProblem() {
    alert("SignalR connection is slow");
}

function notifyUserOfTryingToReconnect() {
    alert("SignalR reconnecting");
}

function notifyUserOfDisconnect() {
    alert("SignalR disconnected");
}


function writeLine(parent, message) {
    try {
        var telem = JSON.parse(message);
        var row = document.createElement("div");
        row.className = "row";
        parent.appendChild(row);

        writeProperty("Name:", telem.name, "col-sm-2", row, true);
        writeProperty("Direction:", telem.direction, "col-sm-2", row, false);
        writeProperty("Unit:", telem.unitId, "col-sm-1", row, false);
        writeProperty("TX:", telem.transactionId, "col-sm-1", row, false);
        writeProperty("", telem.timestamp, "col-sm-3", row, false);
        if (telem.latency !== null && telem.latency !== -1) {
            writeProperty("Latency:", Math.round(telem.latency) + " ms", "col-sm-2", row, false);
        }
    } catch (error) {
        console.error(error);
    }

}


function writeProperty(name, value, className, row, init) {

    var div = getDiv(className);
    row.appendChild(div);

    var smallNode = document.createElement("small");
    div.appendChild(smallNode);
    var proxy = smallNode;

    if (init) {
        var icon = getChevronSpan();
        proxy.appendChild(icon);
    }

    if (name === "Latency:") {
        if (value !== -1) {
            var mark = document.createElement("mark");
            proxy.appendChild(mark);
            proxy = mark;
        } else {
            value = "NA";
        }
    }

    var textNode = getTextNode(name);
    proxy.appendChild(textNode);

    var valueSpan = getValueSpan(value);
    proxy.appendChild(valueSpan);
}

function getDiv(className) {
    var div = document.createElement("div");
    div.className = className;
    return div;
}

function getChevronSpan() {
    var span = document.createElement("span");
    span.className = "glyphicon glyphicon-chevron-right";
    span.style = "padding-right:5px";
    return span;
}

function getTextNode(text) {
    return document.createTextNode(text);
}

function getValueSpan(text) {
    var span = document.createElement("span");
    span.style = "padding-left:5px";
    span.innerHTML = text;
    return span;
}

function toggle(id) {
    var div = document.getElementById(id);
    var span = document.getElementById(id + "-logspan");
    if (span.className === "glyphicon glyphicon-stop") {
        span.className = "glyphicon glyphicon-play";
        connection.invoke("Subscribe", id, false);
    } else {
        span.className = "glyphicon glyphicon-stop";
        connection.invoke("Subscribe", id, true);
    }
}

function toggleApp(id) {
    var div = document.getElementById(id);
    var span = document.getElementById(id + "-app-logspan");
    if (span.className === "glyphicon glyphicon-ok-sign") {
        span.className = "glyphicon glyphicon-ok-circle";
        connection.invoke("SubscribeAppInsights", id, false);
    } else {
        span.className = "glyphicon glyphicon-ok-sign";
        connection.invoke("SubscribeAppInsights", id, true);
    }

}