using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace JHSNetProtocol
{
    internal class JHSServer : IJHSNetworkTransport
    {
        protected Socket _receiveSocket;
        private Dictionary<uint, JHSConnection> m_Connections = new Dictionary<uint, JHSConnection>();
        protected JHSNetworkMessageHandlers m_MessageHandlers = new JHSNetworkMessageHandlers();
        private static int count = 0;

        public static uint IncrementCount()
        {
            int newValue = Interlocked.Increment(ref count);
            return unchecked ((uint)newValue);
        }

        public JHSServer(JHSNetworkMessageHandlers m_MessageHandlersx)
        {
            m_MessageHandlers.ClearMessageHandlers();
            m_MessageHandlers = m_MessageHandlersx;
        }

        public void StartListening()
        {
            IPAddress ipAddress = IPAddress.Parse(NetConfig.IP);
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, NetConfig.Port);
            _receiveSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                _receiveSocket.Bind(localEndPoint);
                _receiveSocket.Listen(100);
                JHSDebug.Log("JHSNetworkServer :: Started to listen :" + ipAddress.ToString() + " Port:" + NetConfig.Port + " Protocol Version:" + NetConfig.Version);
                _receiveSocket.BeginAccept(new AsyncCallback(AcceptCallback), _receiveSocket);
            }
            catch (Exception e)
            {
                if (NetConfig.logFilter >= JHSLogFilter.Developer) JHSDebug.LogError("JHSNetworkServer :: Excepiton:" + e.ToString());
            }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            uint connectionId = IncrementCount();
            JHSConnection per = new JHSConnection
            {
                connectionId = connectionId
            };
            per.Init(false);
            per.SetHandlers(m_MessageHandlers);
            per.StartReceiving(handler);
            lock (m_Connections)
            {
                if (!m_Connections.ContainsKey(connectionId))
                    m_Connections.Add(connectionId, per);
            }
            // Signal the main thread to continue.  
            _receiveSocket.BeginAccept(new AsyncCallback(AcceptCallback), _receiveSocket);
        }

        public void Send(uint connectionId, short msgType, JHSMessageBase msg)
        {
            if (m_Connections.TryGetValue(connectionId, out JHSConnection conection))
            {
                if (conection != null)
                {
                    conection.Send(msgType, msg);
                    return;
                }
            }
            if (NetConfig.logFilter >= JHSLogFilter.Error) { JHSDebug.LogError("JHSNetworkServer :: Failed to send message to connection ID '" + connectionId + ", not found in connection list"); }
        }

        public void SendToAll(short msgType, JHSMessageBase msg)
        {
            try
            {
                JHSConnection[] connections = m_Connections.Values.ToArray();
                for(int i =0; i < connections.Length; i++)
                {
                    if (connections[i] != null && connections[i].ConnectionReady())
                    {
                        connections[i].Send(msgType, msg);
                    }
                }
            }
            catch (Exception e)
            {
                if (NetConfig.logFilter >= JHSLogFilter.Error) JHSDebug.LogError("JHSNetworkServer :: Exception:" + e.ToString());
            }
        }

        public bool Disconnect(JHSConnection con)
        {
            if (con != null)
            {
                uint conId = con.connectionId;
                JHSNetworkServer.PushMessage(new JHSNetworkMessage
                {
                    msgType = InternalMessages.DISCONNECT,
                    conn = con,
                    reader = new JHSNetworkReader()
                });
                JHSStatisiticsManager.Remove(con);
                lock (m_Connections)
                {
                    if(m_Connections.ContainsKey(conId))
                        m_Connections.Remove(conId);
                }
                con.Dispose();
            }
            return true;
        }

        public void SetOperational(bool count)
        {

        }

        public void DoUpdate()
        {

        }

        public JHSConnection StartClient()
        {
            return null;
        }

        public void Stop()
        {
            JHSConnection[] cons = m_Connections.Values.ToArray();
            for (int i = 0; i < cons.Length; i++)
            {
                if (cons[i] != null)
                {
                    Disconnect(cons[i]);
                }
            }

            m_Connections.Clear();
        }

        public void Reset()
        {

        }
    }
}
