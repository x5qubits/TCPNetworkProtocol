using System;
using System.Timers;

namespace JHSNetProtocol
{
    public enum SearchMatchOperations
    {
        NO,
        Search,
        START
    }

    public class SearchMatch : JHSMessageBase
    {
        public SearchMatchOperations op;
        public uint value;
        public string IP = "";
        public short port = 0;

        public override void Deserialize(JHSNetworkReader reader)
        {
            op = (SearchMatchOperations)reader.ReadByte();
            if (op == SearchMatchOperations.Search)
            {
                value = reader.ReadPackedUInt32();
            }
            if (op == SearchMatchOperations.START)
            {
                IP = reader.ReadString();
                port = reader.ReadInt16();
            }
        }

        public override void Serialize(JHSNetworkWriter writer)
        {
            writer.Write((byte)op);
            if (op == SearchMatchOperations.Search)
            {
                writer.WritePackedUInt32(value);
            }

            if (op == SearchMatchOperations.START)
            {
                writer.Write(IP);
                writer.Write(port);
            }
        }
    }

    class Program
    {
        private static Timer timer1;

        static void Main(string[] args)
        {
            Console.Title = "JHS Server Gateway";
            NetConfig.logFilter = JHSLogFilter.Developer;
            JHSNetworkServer.RegisterHandler(100, TESTMSG_RECIVE);
            JHSNetworkServer.Start("0.0.0.0");
            timer1 = new Timer();
            timer1.Elapsed += OnTimedEvent;
            timer1.Interval = 200; // in miliseconds
            timer1.Start();
            Console.ReadKey();
        }

        private static void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
           // JHSNetworkServer.SendToAll(100, new SearchMatch() { IP = "127.0.0.1", port = 1985 });
        }


        private static void TESTMSG_RECIVE(JHSNetworkMessage netMsg)
        {
            uint connectionId = netMsg.conn.connectionId;
            string ip = netMsg.conn.IP;
            SearchMatch packet = netMsg.ReadMessage<SearchMatch>();
            if(packet != null)
            {
                JHSDebug.Log(packet.op.ToString());
                netMsg.conn.Send(100, new SearchMatch() { op = SearchMatchOperations.Search });
                netMsg.conn.Send(100, new SearchMatch() { op= SearchMatchOperations.START, IP = "127.0.0.1", port = 1985 });

              //  JHSNetworkManager.SendToAll(100, new TESTMSGRE() {ClientId = connectionId, Time = packet.Time, TimeServ = JHSTime.TimeStamp });
            }
        }
    }
}
