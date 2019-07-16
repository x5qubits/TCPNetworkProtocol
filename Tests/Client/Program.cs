using JHSNetProtocol;
using System;
using System.Timers;

namespace TEST
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
        private static Timer timer2;

        static void Main(string[] args)
        {
            NetConfig.logFilter = JHSLogFilter.Developer;
            JHSNetworkClient.RegisterHandler(InternalMessages.CONNECTED, CONNECTED_TO_SERVER);
            JHSNetworkClient.RegisterHandler(InternalMessages.DISCONNECT, DISCONNECTED_PERMANENT);
            JHSNetworkClient.RegisterHandler(InternalMessages.DISCONNECT_BUT_WILL_RECONNECT, DISCONNECTED_FROM_SERVER);
            JHSNetworkClient.RegisterHandler(100, TESTMSGREC);
            JHSNetworkClient.Start("127.0.0.1");
            timer1 = new Timer();
            timer1.Elapsed += OnTimedEvent;
            timer1.Interval = 5000; // in miliseconds
            timer1.Start();
            timer2 = new Timer();
            timer2.Elapsed += SendPackets;
            timer2.Interval = 100; // in miliseconds
            timer2.Start();
            Console.WriteLine("Press c to stop it and any key to send msg");
            bool loop = true;
            while (loop == true)
            {
               
                string key = Console.ReadLine();
                switch (key)
                {
                    case "c":
                        loop = false;
                        break;
                    default:
                        JHSNetworkClient.Send(100, new SearchMatch() { op = SearchMatchOperations.NO,  value = 0 });
                        Console.WriteLine("SENT");
                        break;
                }
               
            }

            
        }

        private static void DISCONNECTED_PERMANENT(JHSNetworkMessage netMsg)
        {
            Console.WriteLine("DISCONNECTED_PERMANENT");
        }

        private static void SendPackets(object sender, ElapsedEventArgs e)
        {
            JHSNetworkClient.Update();
        }

        private static float Time = 0;
        private static void TESTMSGREC(JHSNetworkMessage netMsg)
        {
            SearchMatch msg = netMsg.ReadMessage<SearchMatch>();
            if(msg != null)
            {
                    Time = JHSTime.Time + 5;
                    Console.WriteLine(msg.op.ToString());
            }
        }

        private static void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            

            if (JHSNetworkClient.Connected)
                JHSNetworkClient.Send(100, new SearchMatch() { op = SearchMatchOperations.NO });
        }


        private static void DISCONNECTED_FROM_SERVER(JHSNetworkMessage netMsg)
        {
            Console.WriteLine("DISCONNECTED_FROM_SERVER");
        }

        private static void CONNECTED_TO_SERVER(JHSNetworkMessage netMsg)
        {

            if (netMsg.conn != null)
            {
                Console.Title = "Connection:" + netMsg.conn.connectionId;
                Console.WriteLine("CONNECTED_TO_SERVER CONNECTION ID:"+ netMsg.conn.connectionId);
            }
            else
                Console.WriteLine("CONNECTED_TO_SERVER");

        }
    }
}
