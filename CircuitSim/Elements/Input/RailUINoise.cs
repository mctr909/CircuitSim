using System.Drawing;

namespace Circuit.Elements.Input {
    class RailUINoise : RailUI {
        public RailUINoise(Point pos) : base(pos, VoltageElm.WAVEFORM.NOISE) { }
    }
}
