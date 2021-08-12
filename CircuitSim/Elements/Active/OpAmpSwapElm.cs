using System.Drawing;

namespace Circuit.Elements.Active {
    class OpAmpSwapElm : OpAmpElm {
        public OpAmpSwapElm(Point pos) : base(pos) {
            mFlags |= FLAG_SWAP;
        }
    }
}
