using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace JamesFrowen.SimpleWeb
{
    internal sealed class Connection : IDisposable
    {
        public const int IdNotSet = -1;

        readonly object disposedLock = new object();

        public TcpClient client;

        public int connId = IdNotSet;

        /// <summary>
        /// Connect request, sent from client to start handshake
        /// <para>Only valid on server</para>
        /// </summary>
        public Request request;
        /// <summary>
        /// RemoteEndpoint address or address from request header
        /// <para>Only valid on server</para>
        /// </summary>
        public string remoteAddress;

        public Stream stream;
        public Thread receiveThread;
        public Thread sendThread;

        public ManualResetEventSlim sendPending = new ManualResetEventSlim(false);
        public ConcurrentQueue<ArrayBuffer> sendQueue = new ConcurrentQueue<ArrayBuffer>();

        public Action<Connection> onDispose;

        volatile bool hasDisposed;

        public Connection(TcpClient client, Action<Connection> onDispose)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.onDispose = onDispose;
        }

        /// <summary>
        /// disposes client and stops threads
        /// </summary>
        public void Dispose()
        {
            Log.Verbose($"Dispose {ToString()}");

            // check hasDisposed first to stop ThreadInterruptedException on lock
            if (hasDisposed) return;

            Log.Info($"Connection Close: {ToString()}");

            lock (disposedLock)
            {
                // check hasDisposed again inside lock to make sure no other object has called this
                if (hasDisposed) return;

                hasDisposed = true;

                // stop threads first so they don't try to use disposed objects
                receiveThread.Interrupt();
                sendThread?.Interrupt();

                try
                {
                    // stream 
                    stream?.Dispose();
                    stream = null;
                    client.Dispose();
                    client = null;
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                sendPending.Dispose();

                // release all buffers in send queue
                while (sendQueue.TryDequeue(out ArrayBuffer buffer))
                    buffer.Release();

                onDispose.Invoke(this);
            }
        }

        public override string ToString()
        {
            if (hasDisposed)
                return $"[Conn:{connId}, Disposed]";
            else
                try
                {
                    System.Net.EndPoint endpoint = client?.Client?.RemoteEndPoint;
                    return $"[Conn:{connId}, endPoint:{endpoint}]";
                }
                catch (SocketException)
                {
                    return $"[Conn:{connId}, endPoint:n/a]";
                }
        }

        /// <summary>
        /// Gets the address based on the <see cref="request"/> and RemoteEndPoint
        /// <para>Called after ServerHandShake is accepted</para>
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal string CalculateAddress()
        {
            var headers = request.Headers;
            if (headers.TryGetValue("X-Forwarded-For", out var forwardFor))
            {
                var ips = forwardFor.ToString().Split(',');
                var actualClientIP = ips[0];
                // Remove the port number from the address
                string addressWithoutPort = remoteAddress.Split(':')[0];
                return addressWithoutPort;
            }

            return client.Client.RemoteEndPoint.ToString();
        }
    }
}
