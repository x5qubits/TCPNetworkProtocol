# TCPNetworkProtocol
A tcp server client implementation for C# and Unity 3d.

===Usage===

Server Side Example:


        static void Main(string[] args)
        {
            //Register custom handler for message id 100
            JHSNetworkServer.RegisterHandler(100, TESTMSG_RECIVE); 
            //Start Server
            JHSNetworkServer.Start("0.0.0.0"); 
            
        }
        
        private static void TESTMSG_RECIVE(JHSNetworkMessage netMsg)
        {
            uint connectionId = netMsg.conn.connectionId; // Connection id asigned by server
            ExampleMessage packet = netMsg.ReadMessage<ExampleMessage>(); // Read packet
            if(packet != null)
            {
                netMsg.conn.Send(100, packet); //Echo back the packet 
            }
        }


Client side Example:



        static void Main(string[] args)
        {
            //Subscribe to on conencted succesfuly to the server.
            JHSNetworkClient.RegisterHandler(InternalMessages.CONNECTED, CONNECTED_TO_SERVER);
            //Subscribe to disconected from server
            JHSNetworkClient.RegisterHandler(InternalMessages.DISCONNECT, DISCONNECTED_PERMANENT);
            //Subscribe to disconected from server but will reconnect n times based on your configs
            JHSNetworkClient.RegisterHandler(InternalMessages.DISCONNECT_BUT_WILL_RECONNECT, DISCONNECTED_FROM_SERVER);
            //Subscirbe to custom packet handler with id 100
            JHSNetworkClient.RegisterHandler(100, OnTestMsgRec);
            //Connect to server
            JHSNetworkClient.Start("127.0.0.1");

            //This program is made for unity 3d so it has a static queue, therefor we need to update that queue in a normal C# application.
            //So we create a timer:
            var timer1 = new Timer();
            timer1.Elapsed += JHSNetworkClientUpdate;
            timer1.Interval = 200; // in miliseconds
            timer1.Start();                
            
        }
        
        private static void CONNECTED_TO_SERVER(JHSNetworkMessage netMsg)
        {
            //Connected to server so we send an example packet
            JHSNetworkClient.Send(100, new ExampleMessage() { op = 1234 });
        }
        
        private static void JHSNetworkClientUpdate(object sender, ElapsedEventArgs e)
        {
            JHSNetworkClient.Update();
        }
        
        private static void OnTestMsgRec(JHSNetworkMessage netMsg)
        {
            ExampleMessage msg = netMsg.ReadMessage<ExampleMessage>();
            if(msg != null)
            {
                //
            }
        }
            
            
            
