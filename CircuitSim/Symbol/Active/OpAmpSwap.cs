using System.Drawing;

namespace Circuit.Symbol.Active {
    class OpAmpSwap : OpAmp {
        public OpAmpSwap(Point pos) : base(pos) {
            mFlags |= FLAG_SWAP;
        }
    }
}
