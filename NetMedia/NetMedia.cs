using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IPVideoCall.NetMedia
{
    abstract class NetMedia
    {
        protected TcpListener mListener;
        protected TcpClient mCurrentClient;
        protected NetworkStream mCurrentNS;

        public virtual bool IsStopped
        {
            get
            {
                return true;
            }
        }

        public abstract TcpClient Connect(String ip, int port);
        public abstract void Start();
        public abstract void Stop();


    }
}
