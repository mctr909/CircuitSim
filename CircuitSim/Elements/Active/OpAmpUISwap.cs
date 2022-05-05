using System.Drawing;

namespace Circuit.Elements.Active {
    class OpAmpUISwap : OpAmpUI {
        public OpAmpUISwap(Point pos) : base(pos) {
            mFlags |= FLAG_SWAP;
        }
    }
}
