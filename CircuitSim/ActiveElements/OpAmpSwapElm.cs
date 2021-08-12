using System.Drawing;

namespace Circuit.ActiveElements {
    class OpAmpSwapElm : OpAmpElm {
        public OpAmpSwapElm(Point pos) : base(pos) {
            mFlags |= FLAG_SWAP;
        }
    }
}
