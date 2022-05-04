using System.Drawing;

namespace Circuit.Elements.Input {
    class ACRailElm : RailElm {
        public ACRailElm(Point pos) : base(pos, VoltageElmE.WAVEFORM.AC) { }
    }
}
