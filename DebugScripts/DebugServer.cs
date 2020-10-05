using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    [RequireComponent(typeof(SimpleWebTransport))]
    public class DebugServer : MonoBehaviour
    {
        public Vector2 guiPosition;
        private SimpleWebTransport transport;

        private void Start()
        {
            transport = GetComponent<SimpleWebTransport>();
            transport.OnServerConnected.AddListener(onConnect);
            transport.OnServerDataReceived.AddListener(onData);
            transport.OnServerDisconnected.AddListener(onDisconnect);
            transport.OnServerError.AddListener(onError);
            transport.ServerStart();
        }

        private void onConnect(int connId)
        {
            Debug.Log($"onConnect:{connId}");
        }

        private void onData(int connId, ArraySegment<byte> data, int channel)
        {
            string str = BitConverter.ToString(data.Array, data.Offset, data.Count);
            Debug.Log($"onData:{connId} data:{str}");
        }

        private void onDisconnect(int connId)
        {
            Debug.LogWarning($"onDisconnect:{connId}");
        }

        private void onError(int connId, Exception e)
        {
            Debug.LogError($"onError:{connId}, {e}");
        }


        public void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(guiPosition, new Vector2(500, 500))))
            {
                if (GUILayout.Button("Send Message"))
                {
                    transport.ServerSend(new List<int>() { 1 }, Channels.DefaultReliable, new ArraySegment<byte>(new byte[] { 1, 2, 4, 8 }));
                }
            }
        }
    }
}
