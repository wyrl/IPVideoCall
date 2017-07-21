using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPVideoCall
{
    class ServerTest
    {
        private static UdpClient udpClient;
        public static void Start()
        {
            udpClient = new UdpClient();
        }

    }
}
