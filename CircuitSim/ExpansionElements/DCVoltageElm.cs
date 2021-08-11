using System.Drawing;

namespace Circuit.Elements {
    class DCVoltageElm : VoltageElm {
        public DCVoltageElm(Point pos) : base(pos, WAVEFORM.DC) { }
    }
}
