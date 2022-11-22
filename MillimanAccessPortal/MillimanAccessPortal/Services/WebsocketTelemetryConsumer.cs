using System;
using Yarp.Telemetry.Consumption;

namespace MillimanAccessPortal.Services
{
    public class WebsocketTelemetryConsumer : IWebSocketsTelemetryConsumer
    {
        //
        // Summary:
        //     Called when a WebSockets connection is closed.
        //
        // Parameters:
        //   timestamp:
        //     Timestamp when the event was fired.
        //
        //   establishedTime:
        //     Timestamp when the connection upgrade completed.
        //
        //   closeReason:
        //     The reason the WebSocket connection closed.
        //
        //   messagesRead:
        //     Messages read by the destination server.
        //
        //   messagesWritten:
        //     Messages sent by the destination server.
        public void OnWebSocketClosed(DateTime timestamp, DateTime establishedTime, WebSocketCloseReason closeReason, long messagesRead, long messagesWritten)
        {
            DateTime dateTime = timestamp;
        }
    }
}
