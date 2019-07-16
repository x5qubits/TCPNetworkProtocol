using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace JHSNetProtocol
{
    public class JHSConnection : IDisposable
    {
        public Socket m_socket;
        public float lastReceivedTime;
        float lastSentTime;
        protected IPEndPoint tcpEndPoint;
        public PerStage stage = PerStage.NotConnected;
        public uint connectionId = 0;
        Dictionary<short, JHSNetworkMessageDelegate> m_MessageHandlersDict = new Dictionary<short, JHSNetworkMessageDelegate>();
        const int k_MaxMessageLogSize = 150;
        const int MaxOutGowingMsg = 50;
        public bool m_Disposed = false;
        JHSPacketFarmer PacketFarmer;
        internal JHSNetworkMessageHandlers m_MessageHandlers;
        public int BytesSent = 0;
        public int BitesRev = 0;
        public int PacketsSend = 0;
        public int PacketsRec = 0;
        public int ReadError = 0;
        public int SendError = 0;
        public bool isClient = false;
        readonly object m_lockread = new object();
        readonly object m_lockwrite = new object();
        byte[] m_Connbuffer;
        AsyncCallback asyncrec;
        AsyncCallback asyncsend;
        EncDec Crypt;
        public bool IsConnected { get { return m_socket != null && m_socket.Connected; } }

        public JHSConnection()
        {
            asyncrec = new AsyncCallback(EndReceive);
            asyncsend = new AsyncCallback(EndSend);
            PacketFarmer = new JHSPacketFarmer();
            m_Connbuffer = new byte[1024];
            Crypt = new EncDec();
        }

        ~JHSConnection()
        {
            Dispose(false);
        }

        public bool ConnectionReady()
        {
            return stage == PerStage.Connected && m_socket != null && m_socket.Connected;
        }

        public void Init(bool isClient)
        {
            this.isClient = isClient;
        }

        public void StartReceiving(Socket socket)
        {
            if (socket != null)
            {
                m_socket = socket;
            }

            if (m_socket != null && m_socket.Connected)
            {
                stage = PerStage.Verifying;
                tcpEndPoint = (IPEndPoint)m_socket.RemoteEndPoint;
                try
                {
                    m_socket.BeginReceive(m_Connbuffer, 0, m_Connbuffer.Length, SocketFlags.None, asyncrec, null);
                }
                catch
                {
                    Disconnect();
                }
            }
        }
    #region RECEIVE
        private void EndReceive(IAsyncResult result)
        {
            if (stage == PerStage.NotConnected) return;
            lock (m_lockread)
            {
                try
                {
                    int len = m_socket.EndReceive(result);
                    if (len == 0) { Disconnect(); return; }

                    byte[] buf = Crypt.Decode(this.m_Connbuffer, len);
                    for (int i = 0; i < buf.Length; i++)
                    {
                        JHSNetworkReader ds = PacketFarmer.Accumulate(buf[i]);
                        if (ds != null)
                        {
                            if (HandleReader(ds))
                            {
                                if (NetConfig.UseStatistics)
                                {
                                    PacketsRec += 1;
                                    BitesRev += len;
                                }
                            }
                        }
                    }
                    m_socket.BeginReceive(this.m_Connbuffer, 0, this.m_Connbuffer.Length, SocketFlags.None, asyncrec, null);
                }
                catch (ObjectDisposedException)
                {
                    // do nothing
                }
                catch (SocketException sk)
                {
                    if (NetConfig.logFilter >= JHSLogFilter.Developer) JHSDebug.LogError("JHSConnection :: Excepiton:" + sk.GetErrorCode());
                    Disconnect();
                }
                catch (Exception ex)
                {
                    if (NetConfig.logFilter >= JHSLogFilter.Developer) JHSDebug.LogError("JHSConnection :: Excepiton:" + ex.ToString());
                    Disconnect();
                }
            }
        }

        protected bool HandleReader(JHSNetworkReader reader)
        {
            try
            {
                ushort sz = reader.ReadUInt16();
                short msgType = reader.ReadInt16();
                byte[] msgBuffer = reader.ReadBytes(sz);
                if (isClient)
                {
                    JHSNetworkReader msgReader = new JHSNetworkReader(msgBuffer);
                    if (m_MessageHandlersDict.ContainsKey(msgType))
                    {
                        JHSNetworkClient.PushMessage(new JHSNetworkMessage()
                        {
                            msgType = msgType,
                            reader = msgReader,
                            conn = this
                        });
                }
                else
                {
                    if (NetConfig.logFilter >= JHSLogFilter.Error) { JHSDebug.LogError("JHSConnection :: Unknown message ID " + msgType + " connId:" + connectionId); }
                    if (NetConfig.UseStatistics)
                        ReadError += 1;
                }
            }
                else
                {
                JHSNetworkReader msgReader = new JHSNetworkReader(msgBuffer);
                JHSNetworkMessageDelegate msgDelegate = null;
                if (m_MessageHandlersDict.ContainsKey(msgType))
                {
                    msgDelegate = m_MessageHandlersDict[msgType];
                }
                if (msgDelegate != null)
                {
                    msgDelegate(new JHSNetworkMessage()
                    {
                        msgType = msgType,
                        reader = msgReader,
                        conn = this
                    });
                }
                else
                {
                    if (NetConfig.logFilter >= JHSLogFilter.Error) { JHSDebug.LogError("JHSConnection :: Unknown message ID " + msgType + " connId:" + connectionId); }
                    if (NetConfig.UseStatistics)
                        ReadError += 1;
                }
            }
        }
            catch { }
            return true;
        }
    #endregion

    #region SEND
    Queue<SendState> ToSend = new Queue<SendState>();
    bool sendbegined;
    public void Send(short msgType, JHSMessageBase packet)
    {
        try
        {
            lock (ToSend)
                ToSend.Enqueue(new SendState() { msgType = msgType, packet = packet });

            if (sendbegined) return;
            sendbegined = true;
            BeginSend();
        }
        catch
        {
            if (NetConfig.UseStatistics)
                SendError += 1;
        }
    }

    protected void BeginSend()
    {
        if (ToSend.Count == 0)
        {
            sendbegined = false;
            return;
        }
        try
        {
            SendState state;
            lock (ToSend)
            {
                state = ToSend.Dequeue();
            }
            byte[] buf = Crypt.Encode(PacketFarmer.ToBytes(state.msgType, state.packet));
            m_socket.BeginSend(buf, 0, buf.Length, SocketFlags.None, asyncsend, null);
        }
        catch (ObjectDisposedException)
        {
            // do nothing
        }
        catch (SocketException)
        {
            Disconnect();
        }
        catch (Exception e)
        {
            if (NetConfig.logFilter >= JHSLogFilter.Error) JHSDebug.LogError("JHSConnection :: Exception: " + e.ToString());
            Disconnect();
        }
    }

    private void EndSend(IAsyncResult ar)
    {
        try
        {
            int bytesSent = m_socket.EndSend(ar);
            if (NetConfig.UseStatistics)
            {
                BytesSent += bytesSent;
                PacketsSend += 1;
                lastSentTime = JHSTime.Time;
            }
            BeginSend();
            if (NetConfig.logFilter >= JHSLogFilter.Developer) JHSDebug.Log(string.Format("JHSConnection :: Sent {0} bytes.", bytesSent));

        }
        catch (ObjectDisposedException)
        {
            // do nothing
        }
        catch (SocketException)
        {
            Disconnect();
        }
        catch (Exception e)
        {
            if (NetConfig.logFilter >= JHSLogFilter.Error) JHSDebug.LogError("JHSConnection :: Exception: " + e.ToString());
            Disconnect();
        }
    }
    #endregion

    public void Dispose()
    {
        Dispose(true);
        // Take yourself off the Finalization queue
        // to prevent finalization code for this object
        // from executing a second time.
        GC.SuppressFinalize(this);
    }

    public void Disconnect()
    {
        stage = PerStage.NotConnected;
        try
        {
            if (m_socket != null)
                m_socket.Close();
        }
        catch { }
        if (isClient)
            JHSNetworkClient.Disconnect(this);
        else
            JHSNetworkServer.Disconnect(this);
    }

    protected void Dispose(bool disposing)
    {
        // Check to see if Dispose has already been called.
        if (!m_Disposed)
        {
            // If disposing equals true, dispose all managed 
            // and unmanaged resources.
            if (disposing)
            {
                // Dispose managed resources.
                PacketFarmer = null;
                m_Connbuffer = null;
                m_MessageHandlers = null;
                m_MessageHandlersDict = null;
                m_socket = null;
                tcpEndPoint = null;
            }
        }
        m_Disposed = true;
    }

    internal void SetHandlers(JHSNetworkMessageHandlers handlers)
    {
        m_MessageHandlers = handlers;
        m_MessageHandlersDict = handlers.GetHandlers();
    }

    public void Reset()
    {
        ReadError = 0;
        SendError = 0;
        BytesSent = 0;
        BitesRev = 0;
        PacketsSend = 0;
        PacketsRec = 0;
        stage = PerStage.NotConnected;
    }

    public string IP
    {
        get
        {
            try
            {
                return tcpEndPoint.Address.ToString();
            }
            catch
            { }
            return "";
        }
    }

    public override string ToString()
    {
        return "connectionId:" + connectionId + " BytesSent:" + BytesSent + " BitesRev:" + BitesRev + " PacketsSend:" + PacketsSend + " PacketsRec:" + PacketsRec + " ReadErrors:" + ReadError + " SendError:" + SendError;
    }
}
}
