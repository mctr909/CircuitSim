using System.Drawing;

namespace Circuit.Elements {
    class OpAmpSwapElm : OpAmpElm {
        public OpAmpSwapElm(Point pos) : base(pos) {
            mFlags |= FLAG_SWAP;
        }
    }
}
