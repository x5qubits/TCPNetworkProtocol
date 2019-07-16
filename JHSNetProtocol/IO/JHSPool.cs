using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace JHSNetProtocol
{
    public class StateObject
    {
        public Socket workSocket = null;
        public const int BUFFER_SIZE = 1024 * 32;
        public byte[] buffer = new byte[BUFFER_SIZE];
        public int sz = 0;
        public int read = 0;
        public JHSNetworkReader reader;


        public StateObject()
        {
            reader = new JHSNetworkReader(buffer);
        }

        public void SetSocket(Socket _workSocket)
        {
            workSocket = _workSocket;
        }

        public void Reset()
        {
            sz = 0;
            read = 0;
            reader.SeekZero();
        }
    }

    public struct SendState
    {
        public short msgType;
        public JHSMessageBase packet;
    }
}
