using System.Drawing;

namespace Circuit.Elements.Input {
    class ACVoltageElm : VoltageElm {
        public ACVoltageElm(Point pos) : base(pos, VoltageElmE.WAVEFORM.AC) { }
    }
}
