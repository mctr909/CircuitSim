using System.Drawing;

namespace Circuit.Elements.Input {
    class VoltageUIDC : VoltageUI {
        public VoltageUIDC(Point pos) : base(pos, VoltageElm.WAVEFORM.DC) { }
    }
}
