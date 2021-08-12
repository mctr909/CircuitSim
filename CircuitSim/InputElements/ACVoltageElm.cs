using System.Drawing;

namespace Circuit.InputElements {
    class ACVoltageElm : VoltageElm {
        public ACVoltageElm(Point pos) : base(pos, WAVEFORM.AC) { }
    }
}
