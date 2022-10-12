using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class RailAC : Rail {
        public RailAC(Point pos) : base(pos, ElmVoltage.WAVEFORM.AC) { }
    }
}
