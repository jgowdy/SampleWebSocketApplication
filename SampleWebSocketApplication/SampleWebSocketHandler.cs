using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace SampleWebSocketApplication
{
    public class SampleWebSocketHandler : IHttpHandler
    {
        private const int MaxMessageSize = 1024;
        private static readonly byte[] HeartbeatMessageBytes = Encoding.UTF8.GetBytes("HEARTBEAT");

        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return true; }
        }

        /// <summary>
        /// This function gets all HTTP requests (websocket or not)
        /// </summary>
        public void ProcessRequest(HttpContext context)
        {
            //Route the request
            switch (context.Request.Path)
            {
                case "/socket":
                    if (context.IsWebSocketRequest)
                        context.AcceptWebSocketRequest(HandleWebSocket);
                    else
                        context.Response.StatusCode = 400;
                    break;
                case "/":
                    SendFile(context, "/index.html");
                    break;
                case "/index.html":
                    SendFile(context, "/index.html");
                    break;
                case "/SampleWebSocketClient.js":
                    SendFile(context, "/SampleWebSocketClient.js");
                    break;
                default:
                    context.Response.StatusCode = 404;
                    break;
            }
        }

        /// <summary>
        /// Simple function to send static files
        /// </summary>
        public void SendFile(HttpContext context, string path)
        {
            if (context.Request.PhysicalApplicationPath == null)
                throw new InvalidOperationException("Unknown physical application path");
            context.Response.TransmitFile(Path.Combine(context.Request.PhysicalApplicationPath, path));
        }

        /// <summary>
        /// This is our callback for handling websocket connections
        /// This is based largely on Paul Batum's example here:
        /// http://paulbatum.github.io/WebSocket-Samples/AspNetWebSocketEcho/
        /// </summary>
        private async Task HandleWebSocket(AspNetWebSocketContext webSocketContext)
        {
            var receiveBuffer = new byte[MaxMessageSize];
            var socket = webSocketContext.WebSocket;
            var fullArraySegment = new ArraySegment<byte>(receiveBuffer);

            //Simulate server side events with this 10 second heartbeat timer
            var timer = SetupHeartbeatTimer(webSocketContext, TimeSpan.FromSeconds(10));

            try
            {
                //This async/await while loop runs as long as the connection is open, receiving new messages
                while (socket.State == WebSocketState.Open)
                {
                    var receiveResult = await socket.ReceiveAsync(fullArraySegment, CancellationToken.None);

                    //Switch on websocket message type
                    switch (receiveResult.MessageType)
                    {
                        //Client requested socket close
                        case WebSocketMessageType.Close:
                            await
                                socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                                    CancellationToken.None);
                            break;

                        //We don't support binary frames/messages (although we could if we wanted to)
                        case WebSocketMessageType.Binary:
                            await
                                socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Binary frames not supported",
                                    CancellationToken.None);
                            break;

                        //Text frames/messages are what we're most interested in
                        case WebSocketMessageType.Text:
                            await HandleTextMessage(webSocketContext, receiveResult, socket, receiveBuffer);
                            break;

                        //This should never happen, but if it does, handle it gracefully
                        default:
                            await
                                socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Unknown message type",
                                    CancellationToken.None);
                            break;
                    }
                }
            }
            finally
            {
                //When the connection closes, kill the timer
                timer.Change(-1, -1);
                timer.Dispose();
            }

        }

        /// <summary>
        /// This function sets up a timer at the specified interval that will transmit "HEARTBEAT" to the client
        /// </summary>
        private Timer SetupHeartbeatTimer(AspNetWebSocketContext webSocketContext, TimeSpan interval)
        {
            var fullHeartbeatSegment = new ArraySegment<byte>(HeartbeatMessageBytes);
            var socket = webSocketContext.WebSocket;

            return new Timer(async o =>
            {
                try
                {
                    await
                        socket.SendAsync(fullHeartbeatSegment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception e)
                {
                    Trace.WriteLine("Exception in timer: " + e.Message);
                }
            }, null, TimeSpan.FromSeconds(1), interval);
        }

        private async Task HandleTextMessage(AspNetWebSocketContext webSocketContext, WebSocketReceiveResult receiveResult,
            WebSocket socket, byte[] receiveBuffer)
        {
            int count = receiveResult.Count;
            while (receiveResult.EndOfMessage == false)
            {
                if (count >= MaxMessageSize)
                {
                    await
                        socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "Message too large",
                            CancellationToken.None);
                    return;
                }
                receiveResult =
                    await
                        socket.ReceiveAsync(
                            new ArraySegment<byte>(receiveBuffer, count, receiveBuffer.Length - count),
                            CancellationToken.None);
                count += receiveResult.Count;
            }
            string textMessage = Encoding.UTF8.GetString(receiveBuffer, 0, count);
            await ProcessTextMessage(webSocketContext, textMessage);
        }

        private async Task ProcessTextMessage(AspNetWebSocketContext webSocketContext, string textMessage)
        {
            //Do something with the text message received.

            //For testing, add a special close message that causes the socket to be closed
            if (textMessage == "close")
            {
                await
                    webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                        CancellationToken.None);
                return;
            }

            //For demo purposes, we just send it back in all caps.
            var output = Encoding.UTF8.GetBytes(textMessage.ToUpper());
            await
                webSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(output), WebSocketMessageType.Text, true,
                    CancellationToken.None);
        }

        #endregion
    }
}
