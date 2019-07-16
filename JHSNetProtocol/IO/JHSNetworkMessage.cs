namespace JHSNetProtocol
{
    public delegate void JHSNetworkMessageDelegate(JHSNetworkMessage netMsg);

    public class JHSNetworkMessage
    {
        public const int MaxMessageSize = (64 * 1024) - 1;

        public short msgType;
        public JHSConnection conn;
        public JHSNetworkReader reader;

        public static string Dump(byte[] payload, int sz)
        {
            string outStr = "[";
            for (int i = 0; i < sz; i++)
            {
                outStr += (payload[i] + " ");
            }
            outStr += "]";
            return outStr;
        }

        public TMsg ReadMessage<TMsg>() where TMsg : JHSMessageBase, new()
        {
            var msg = new TMsg();
            msg.Deserialize(reader);
            return msg;
        }

        public void ReadMessage<TMsg>(TMsg msg) where TMsg : JHSMessageBase
        {
            msg.Deserialize(reader);
        }
    }
    public class ConnectionStatus : JHSMessageBase
    {
        public bool WillReconnect = false;
        public int reconnectTry = 0;

        public override void Deserialize(JHSNetworkReader reader)
        {
            WillReconnect = reader.ReadBoolean();
            reconnectTry = reader.ReadByte();
        }

        public override void Serialize(JHSNetworkWriter writer)
        {
            writer.Write(WillReconnect);
            writer.Write((byte)reconnectTry);
        }
    }
}
