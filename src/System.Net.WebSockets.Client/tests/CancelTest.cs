// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class CancelTest : ClientWebSocketTestBase
    {
        public CancelTest(ITestOutputHelper output) : base(output) { }

        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ConnectAsync_Cancel_ThrowsWebSocketExceptionWithMessage(Uri server)
        {
            using (var cws = new ClientWebSocket())
            {
                var cts = new CancellationTokenSource(500);

                var ub = new UriBuilder(server);
                ub.Query = "delay10sec";

                WebSocketException ex =
                    await Assert.ThrowsAsync<WebSocketException>(() => cws.ConnectAsync(ub.Uri, cts.Token));
                Assert.Equal(
                    ResourceHelper.GetExceptionMessage("net_webstatus_ConnectFailure"),
                    ex.Message);
                Assert.Equal(WebSocketError.Success, ex.WebSocketErrorCode);
                Assert.Equal(WebSocketState.Closed, cws.State);
            }
        }

        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task SendAsync_Cancel_Success(Uri server)
        {
            await TestCancellation((cws) =>
            {
                var cts = new CancellationTokenSource(5);
                return cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);
            }, server);
        }

        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ReceiveAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);
                var cts = new CancellationTokenSource(5);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                await cws.ReceiveAsync(segment, cts.Token);
            }, server);
        }

        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);
                var cts = new CancellationTokenSource(5);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "CancelClose", cts.Token);
            }, server);
        }

        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task CloseOutputAsync_Cancel_Success(Uri server)
        {
            await TestCancellation(async (cws) =>
            {

                var cts = new CancellationTokenSource(5);
                var ctsDefault = new CancellationTokenSource(TimeOutMilliseconds);

                await cws.SendAsync(
                    WebSocketData.GetBufferFromText(".delay5sec"),
                    WebSocketMessageType.Text,
                    true,
                    ctsDefault.Token);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                await cws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "CancelShutdown", cts.Token);
            }, server);
        }

        [ConditionalTheory(nameof(WebSocketsSupported)), MemberData(nameof(EchoServers))]
        public async Task ReceiveAsync_CancelAndReceive_ThrowsWebSocketExceptionWithMessage(Uri server)
        {
            using (ClientWebSocket cws = await WebSocketHelper.GetConnectedWebSocket(server, TimeOutMilliseconds, _output))
            {
                var cts = new CancellationTokenSource(500);

                var recvBuffer = new byte[100];
                var segment = new ArraySegment<byte>(recvBuffer);

                try
                {
                    await cws.ReceiveAsync(segment, cts.Token);
                    Assert.True(false, "Receive should not complete.");
                }
                catch (OperationCanceledException) { }
                catch (ObjectDisposedException) { }
                catch (WebSocketException) { }

                WebSocketException ex = await Assert.ThrowsAsync<WebSocketException>(() =>
                    cws.ReceiveAsync(segment, CancellationToken.None));
                Assert.Equal(
                    ResourceHelper.GetExceptionMessage("net_WebSockets_InvalidState", "Aborted", "Open, CloseSent"),
                    ex.Message);
            }
        }
    }
}
