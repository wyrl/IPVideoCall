using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPVideoCall
{
    public class CameraMovementArgs : EventArgs
    {
        private CameraMovement.Type t;
        private int deg;
        public CameraMovementArgs(CameraMovement.Type type, int deg)
        {
            this.t = type;
            this.deg = deg;
        }
        public CameraMovement.Type Type { get { return t; } }
        public int Degree { get { return deg; } }
    }
}
