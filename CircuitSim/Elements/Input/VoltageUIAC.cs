using System.Drawing;

namespace Circuit.Elements.Input {
    class VoltageUIAC : VoltageUI {
        public VoltageUIAC(Point pos) : base(pos, VoltageElm.WAVEFORM.AC) { }
    }
}
