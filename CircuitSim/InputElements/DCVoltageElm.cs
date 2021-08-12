using System.Drawing;

namespace Circuit.InputElements {
    class DCVoltageElm : VoltageElm {
        public DCVoltageElm(Point pos) : base(pos, WAVEFORM.DC) { }
    }
}
