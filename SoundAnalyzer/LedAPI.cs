using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SoundAnalyzer
{
    static class LedAPI
    {
        private static readonly UdpClient Client = new UdpClient("192.168.100.50", 5001);

        public static void RealTime(byte[] values, bool suddenChange)
        {
            byte header = (byte)values.Length;
            header += (byte)(Convert.ToByte(suddenChange) << 7);

            byte[] newValues = new byte[values.Length + 1];
            newValues[0] = header;
            values.CopyTo(newValues, 1);

            Client.Send(newValues, values.Length + 1);
        }
    }
}
