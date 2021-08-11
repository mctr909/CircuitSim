using System.Drawing;

namespace Circuit.Elements {
    class ACRailElm : RailElm {
        public ACRailElm(Point pos) : base(pos, WAVEFORM.AC) { }
    }
}
