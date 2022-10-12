using System.Drawing;

namespace Circuit.UI.Active {
    class OpAmpSwap : OpAmp {
        public OpAmpSwap(Point pos) : base(pos) {
            DumpInfo.Flags |= FLAG_SWAP;
        }
    }
}
