using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JHSNetProtocol
{
    /// <summary>
    /// Thread safe class mostly for unity
    /// </summary>
    public class JHSNetworkClient
    {

        public static bool hasStarted = false;
        static object s_Sync = new object();
        static volatile JHSNetworkClient s_Instance;
        public static JHSNetworkClient Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_Sync)
                    {
                        if (s_Instance == null)
                        {
                            s_Instance = new JHSNetworkClient();
                        }
                    }
                }
                return s_Instance;
            }
        }
        public bool ClientConnected = false;
        public static bool Connected => Instance.ClientConnected;
        internal Queue<JHSNetworkMessage> recieved = new Queue<JHSNetworkMessage>();
        public JHSConnection connection;

        private IJHSNetworkTransport m_activeTransport = null;
        private JHSNetworkMessageHandlers m_MessageHandlers = new JHSNetworkMessageHandlers();

        #region EXPOSED
        public static void Start(string ip, int port = -1)
        {
            Instance.StartClient(ip, port);
        }

        public static void Start()
        {
            Instance.StartClient();
        }

        public static void Send(short msgType, JHSMessageBase msg, bool diff = false)
        {
            Instance.InternalSend(msgType, msg);
        }

        public static void PushMessage(JHSNetworkMessage msg)
        {
            Instance.AddMsg(msg);
        }

        public static void RegisterHandler(short msgType, JHSNetworkMessageDelegate handler)
        {
            Instance.m_MessageHandlers.RegisterHandlerSafe(msgType, handler);
        }

        public static void CleanHandlers()
        {
            Instance.ClearMessageHandlers();
        }

        public static void RemoveHandler(short msgType)
        {
            Instance.RemoveHandlerInternal(msgType);
        }

        public static void DisableReconnect()
        {
            Instance.SetOperational(false);
        }

        public static void Stop()
        {
            Instance.StopConnecting();
        }

        public static void Update()
        {
            Instance.UpdateRecieve();
        }

        public static void Disconnect(JHSConnection con)
        {
            Instance.Discconnectx(con);
        }

        public static void ResetForReconnect()
        {
            Instance.Reset();
        }

        #endregion

        #region INTERNAL
        internal void Reset()
        {
            if (m_activeTransport != null)
            {
                m_activeTransport.Reset();
            }
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

        internal void Discconnectx(JHSConnection con)
        {
            if (m_activeTransport != null)
            {
                m_activeTransport.Disconnect(con);
                if (NetConfig.logFilter >= JHSLogFilter.Log && con != null) JHSDebug.Log("JHSNetworkManager :: Disconnected :" + con.connectionId);
            }
        }

        internal bool StopConnecting()
        {
            if (m_activeTransport != null)
            {
                m_activeTransport.Stop();
                return true;
            }

            return false;
        }
     
        internal void AddMsg(JHSNetworkMessage msg)
        {
            lock (recieved)
                recieved.Enqueue(msg);
        }

        internal void StartClient(string ip, int port = -1)
        {
            NetConfig.IP = ip;
            if (port != -1)
                NetConfig.Port = port;
            StartClient();
        }

        internal void StartClient()
        {
            INIT();

            if (m_activeTransport == null)
                m_activeTransport = new JHSClient(m_MessageHandlers);

            m_activeTransport.SetOperational(true);
            m_activeTransport.StartClient();
        }

        internal void RemoveHandlerInternal(short msgType)
        {
            m_MessageHandlers.UnregisterHandler(msgType);
        }

        internal void ClearMessageHandlers()
        {
            m_MessageHandlers.ClearMessageHandlers();
        }

        internal void RegisterInternalHandlers()
        {
            m_MessageHandlers.RegisterHandler(InternalMessages.HeandShake_Client, HandleClientHandShake);
        }

        internal void HandleClientHandShake(JHSNetworkMessage netMsg)
        {
            HandShakeMsg packet = netMsg.ReadMessage<HandShakeMsg>();
            if (packet != null)
            {
                if (packet.OP == 0) //VER VERSION
                {
                    netMsg.conn.connectionId = packet.Version;
                    netMsg.conn.stage = PerStage.Connecting;
                    HandShakeMsg p = new HandShakeMsg
                    {
                        Version = (uint)NetConfig.Key,
                        OP = 1
                    };
                    netMsg.conn.Send(InternalMessages.HeandShake_Server, p);

                }
                else if (packet.OP == 1)
                {
                    if (packet.Version == NetConfig.Key)
                    {
                        netMsg.conn.stage = PerStage.Connected;
                        HandShakeMsg p = new HandShakeMsg
                        {
                            Version = (uint)NetConfig.Key,
                            OP = 2
                        };
                        netMsg.conn.Send(InternalMessages.HeandShake_Server, p);
                        ClientConnected = true;
                        connection = netMsg.conn;
                        PushMessage(new JHSNetworkMessage
                        {
                            msgType = InternalMessages.CONNECTED,
                            conn = netMsg.conn,
                            reader = new JHSNetworkReader()
                        });
                    }
                }
            }
        }

        internal void INIT()
        {
            if (!hasStarted)
            {
                RegisterInternalHandlers();
                hasStarted = true;
            }
        }

        internal void SetOperational(bool isOperational)
        {
            if (m_activeTransport != null)
            {
                m_activeTransport.SetOperational(false);
            }
        }

        internal bool InvokeHandler(JHSNetworkMessage netMsg)
        {
            m_MessageHandlers.GetHandler(netMsg.msgType)?.Invoke(netMsg);
            return true;
        }

        float lastTick = 0;
        internal void UpdateRecieve()
        {
            if (JHSTime.Time > lastTick)
            {
                lastTick = JHSTime.Time + 0.1f;
                lock (recieved)
                {
                   if(recieved.Count > 0)
                        InvokeHandler(recieved.Dequeue());            
                }
            }

            if (m_activeTransport != null)
                m_activeTransport.DoUpdate();
        }
        #endregion
    }
}
