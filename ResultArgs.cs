using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPVideoCall
{
    class ResultArgs : EventArgs
    {
        private int audioLatency;
        public ResultArgs(int audioLatency)
        {
            this.audioLatency = audioLatency;
        }

        public int AudioLatency { get { return audioLatency; } }
    }
}
