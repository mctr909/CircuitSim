using System.Drawing;

namespace Circuit.UI.Active {
    class OpAmpSwap : OpAmp {
        public OpAmpSwap(Point pos) : base(pos) {
            mFlags |= FLAG_SWAP;
        }
    }
}
