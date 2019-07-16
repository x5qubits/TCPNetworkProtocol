using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JHSNetProtocol
{
    class EncDec
    {
        public byte[] Decode(byte[] buf, int len)
        {
            byte[] newbuf = new byte[len];
            Array.Copy(buf, newbuf, len);
            return newbuf;
        }

        public byte[] Encode(byte[] buf)
        {
            return buf;
        }
    }
}
