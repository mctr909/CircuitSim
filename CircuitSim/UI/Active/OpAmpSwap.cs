using System.Drawing;

namespace Circuit.UI.Active {
    class OpAmpSwap : OpAmp {
        public OpAmpSwap(Point pos) : base(pos) {
            _Flags |= FLAG_SWAP;
        }
    }
}
