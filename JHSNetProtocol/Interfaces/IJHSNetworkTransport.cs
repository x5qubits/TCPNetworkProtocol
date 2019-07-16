namespace JHSNetProtocol
{
    interface IJHSNetworkTransport
    {
        JHSConnection StartClient();
        void StartListening();
        bool Disconnect(JHSConnection con);
        void SetOperational(bool isOperational);
        void DoUpdate();
        void Send(uint connectionId, short msgType, JHSMessageBase msg);
        void SendToAll(short msgType, JHSMessageBase msg);
        void Stop();
        void Reset();
    }
}
