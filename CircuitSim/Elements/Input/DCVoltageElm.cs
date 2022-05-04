using System.Drawing;

namespace Circuit.Elements.Input {
    class DCVoltageElm : VoltageElm {
        public DCVoltageElm(Point pos) : base(pos, VoltageElmE.WAVEFORM.DC) { }
    }
}
