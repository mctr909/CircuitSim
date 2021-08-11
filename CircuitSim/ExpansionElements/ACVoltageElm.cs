using System.Drawing;

namespace Circuit.Elements {
    class ACVoltageElm : VoltageElm {
        public ACVoltageElm(Point pos) : base(pos, WAVEFORM.AC) { }
    }
}
