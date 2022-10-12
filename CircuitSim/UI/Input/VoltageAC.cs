using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class VoltageAC : Voltage {
        public VoltageAC(Point pos) : base(pos, ElmVoltage.WAVEFORM.AC) { }
    }
}
