using System.Drawing;

namespace Circuit.Elements.Input {
    class NoiseElm : RailElm {
        public NoiseElm(Point pos) : base(pos, VoltageElmE.WAVEFORM.NOISE) { }
    }
}
