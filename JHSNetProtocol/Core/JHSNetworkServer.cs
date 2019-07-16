using System.Collections.Generic;

namespace JHSNetProtocol
{
    public class JHSNetworkServer
    {
        internal IJHSNetworkTransport m_activeTransport = null;
        internal JHSNetworkMessageHandlers m_MessageHandlers = new JHSNetworkMessageHandlers();
        internal bool hasStarted = false;
        static object s_Sync = new object();
        static volatile JHSNetworkServer s_Instance;
        public static JHSNetworkServer Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_Sync)
                    {
                        if (s_Instance == null)
                        {
                            s_Instance = new JHSNetworkServer();
                        }
                    }
                }
                return s_Instance;
            }
        }

        public static void Start(string ip = null, int port = -1)
        {
            if(ip != null)
                NetConfig.IP = ip;

            if (port != -1)
                NetConfig.Port = port;

            Instance.StartServer();
        }

        public static void Disconnect(JHSConnection con)
        {
            Instance.DisconnectClient(con);
        }

        public static void SendToAll(short msgType, JHSMessageBase msg)
        {
            Instance.InternalSendToAll(msgType, msg);
        }

        public static void Send(short msgType, JHSMessageBase msg)
        {
            Instance.InternalSend(msgType, msg);
        }

        public static void Send(uint connectionId, short msgType, JHSMessageBase msg)
        {
            Instance.InternalSend(connectionId, msgType, msg);

        }

        public static void RegisterHandler(short msgType, JHSNetworkMessageDelegate handler)
        {
            Instance.RegisterHandlerSafe(msgType, handler);
        }

        public static void PushMessage(JHSNetworkMessage msg)
        {
            Instance.InvokeHandler(msg);
        }

        #region INTERNAL
        internal void INIT()
        {
            if (!hasStarted)
            {
                RegisterInternalHandlers();
                hasStarted = true;
            }
        }

        internal void StartServer(string ip, int port = -1)
        {
            NetConfig.IP = ip;
            if (port != -1)
                NetConfig.Port = port;
            StartServer();
        }

        internal void StartServer()
        {
            INIT();
            if (m_activeTransport == null)
                m_activeTransport = new JHSServer(m_MessageHandlers);

            m_activeTransport.SetOperational(true);
            m_activeTransport.StartListening();
        }

        internal void DisconnectClient(JHSConnection con)
        {
            if (m_activeTransport != null)
            {
                m_activeTransport.Disconnect(con);
                if (NetConfig.logFilter >= JHSLogFilter.Log && con != null) JHSDebug.Log("JHSNetworkManager :: Disconnected :" + con.connectionId);
            }
        }

        internal void Connected(JHSConnection con)
        {
            InvokeHandler(new JHSNetworkMessage()
            {
                msgType = InternalMessages.CONNECTED,
                conn = con,
                reader = new JHSNetworkReader()
            });
        }

        internal void RegisterHandlerSafe(short msgType, JHSNetworkMessageDelegate handler)
        {
            m_MessageHandlers.RegisterHandlerSafe(msgType, handler);
        }

        internal void AddConnection(JHSConnection con)
        {
            Connected(con);
            if (NetConfig.logFilter >= JHSLogFilter.Developer) JHSDebug.Log("JHSNetworkManager :: Added Connection To Pool.");
        }

        internal void RegisterHandlerInternal(short msgType, JHSNetworkMessageDelegate handler)
        {
            m_MessageHandlers.RegisterHandler(msgType, handler);
        }

        internal void RegisterInternalHandlers()
        {
            RegisterHandlerInternal(InternalMessages.HeandShake_Server, HandleServerHandShake);
        }

        internal void HandleServerHandShake(JHSNetworkMessage netMsg)
        {
            HandShakeMsg packet = netMsg.ReadMessage<HandShakeMsg>();
            if (packet != null)
            {
                if (packet.OP == 0) //VER VERSION
                {
                    if (packet.Version == NetConfig.Version)
                    {
                        netMsg.conn.stage = PerStage.Verifying;
                        HandShakeMsg p = new HandShakeMsg
                        {
                            Version = netMsg.conn.connectionId,
                            OP = 0
                        };
                        netMsg.conn.Send(InternalMessages.HeandShake_Client, p);
                    }
                    else
                    {
                        netMsg.conn.Disconnect();
                    }
                }
                else if (packet.OP == 1)
                {
                    if (packet.Version == NetConfig.Key)
                    {
                        netMsg.conn.stage = PerStage.Connecting;
                        HandShakeMsg p = new HandShakeMsg
                        {
                            Version = (uint)NetConfig.Key,
                            OP = 1
                        };
                        netMsg.conn.Send(InternalMessages.HeandShake_Client, p);
                    }
                    else
                    {
                        netMsg.conn.Disconnect();
                    }
                }
                else if (packet.OP == 2)
                {
                    if (packet.Version == NetConfig.Key)
                    {
                        netMsg.conn.stage = PerStage.Connected;
                        if (NetConfig.logFilter >= JHSLogFilter.Log) { JHSDebug.Log("JHSNetworkManager :: Connected:" + netMsg.conn.connectionId); }
                        AddConnection(netMsg.conn);
                    }
                    else
                    {
                        netMsg.conn.Disconnect();
                    }
                }
                else
                {
                    netMsg.conn.Disconnect();
                }
            }
        }

        internal void InternalSendToAll(short msgType, JHSMessageBase msg)
        {
            if (m_activeTransport != null)
            {
                m_activeTransport.SendToAll(msgType, msg);
                return;
            }
            if (NetConfig.logFilter >= JHSLogFilter.Error) { JHSDebug.LogError("JHSNetworkManager :: Failed to send message to connection ID '" + 0 + ", not found in connection list"); }
        }

        internal void InternalSend(short msgType, JHSMessageBase msg)
        {
            if (m_activeTransport != null)
            {
                m_activeTransport.Send(0, msgType, msg);
                return;
            }
            if (NetConfig.logFilter >= JHSLogFilter.Error) { JHSDebug.LogError("JHSNetworkManager :: Failed to send message to connection ID '" + 0 + ", not found in connection list"); }
        }

        internal void InternalSend(uint connectionId, short msgType, JHSMessageBase msg)
        {
            if (m_activeTransport != null)
            {
                m_activeTransport.Send(connectionId, msgType, msg);
                return;
            }
            if (NetConfig.logFilter >= JHSLogFilter.Error) { JHSDebug.LogError("JHSNetworkManager :: Failed to send message to connection ID '" + connectionId + ", not found in connection list"); }
        }

        internal bool InvokeHandler(JHSNetworkMessage netMsg)
        {
            m_MessageHandlers.GetHandler(netMsg.msgType)?.Invoke(netMsg);
            return true;
        }

        internal void Disable()
        {
            if (m_activeTransport != null)
            {
                m_activeTransport.SetOperational(false);
            }
        }

        #endregion
    }
}
