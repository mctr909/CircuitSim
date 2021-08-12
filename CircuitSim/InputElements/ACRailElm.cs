using System.Drawing;

namespace Circuit.InputElements {
    class ACRailElm : RailElm {
        public ACRailElm(Point pos) : base(pos, WAVEFORM.AC) { }
    }
}
