SampleWebSocketApplication
==========================

This is an example of how to implement a WebSockets application via an IHttpHandler that uses .NET 4.5 WebSocket support.

Messages sent from the client are echoed in all caps by the server.

If the client sends "close" the server will close the connection.

One second after connection, and every 10 seconds after, the server will send "HEARTBEAT"

This code is based largely on Paul Batum's example here:
http://paulbatum.github.io/WebSocket-Samples/AspNetWebSocketEcho/
