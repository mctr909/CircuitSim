using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class VoltageDC : Voltage {
        public VoltageDC(Point pos) : base(pos, ElmVoltage.WAVEFORM.DC) { }
    }
}
