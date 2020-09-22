﻿using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public abstract class BadClientTestBase : SimpleWebTestBase
    {
        protected const int timeout = 4000;

        protected SimpleWebTransport transport;
        protected int onConnectedCalled;
        protected int onDisconnectedCalled;
        protected int onDataReceived;

        [SetUp]
        public void Setup()
        {
            transport = CreateRelayTransport();
            transport.receiveTimeout = timeout;
            transport.sendTimeout = timeout;
            onConnectedCalled = 0;
            onDisconnectedCalled = 0;
            onDataReceived = 0;

            transport.OnServerConnected.AddListener((_) => onConnectedCalled++);
            transport.OnServerDisconnected.AddListener((_) => onDisconnectedCalled++);
            transport.OnServerDataReceived.AddListener((_, __, ___) => onDataReceived++);
            transport.ServerStart();
        }

        protected static Task<TcpClient> CreateBadClient()
        {
            return Task.Run<TcpClient>(() =>
            {
                try
                {
                    TcpClient client = new TcpClient
                    {
                        SendTimeout = 1000,
                        ReceiveTimeout = 1000
                    };

                    client.Connect("localhost", 7776);

                    return client;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                return null;
            });
        }

        protected static bool HasDisconnected(TcpClient client)
        {
            bool resetOrHasData = client.Client.Poll(-1, SelectMode.SelectRead);
            bool noData = client.Available == 0;
            bool reset = resetOrHasData && noData;

            return reset;
        }

        protected static void WriteBadData(TcpClient client)
        {
            byte[] buffer = Enumerable.Range(1, 10).Select(x => (byte)x).ToArray();
            try
            {
                client.GetStream().Write(buffer, 0, 10);
            }
            catch (IOException) { }
        }
    }
}
