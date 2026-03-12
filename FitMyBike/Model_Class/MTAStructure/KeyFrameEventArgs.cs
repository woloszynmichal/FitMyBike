using FitMyBike.View_Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitMyBike.Model_Class.MTAStructure
{
    public class KeyFrameEventArgs : EventArgs
    {
        public int KeyFrameNumber { get; set; }
        internal KeyFrameResult KeyFrameResult { get; set; }
        internal CyclePhaseDrawer CyclePhaseDrawer { get; set; }
    }
}
