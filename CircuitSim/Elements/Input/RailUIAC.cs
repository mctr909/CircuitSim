using System.Drawing;

namespace Circuit.Elements.Input {
    class RailUIAC : RailUI {
        public RailUIAC(Point pos) : base(pos, VoltageElm.WAVEFORM.AC) { }
    }
}
