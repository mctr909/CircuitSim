using System.Drawing;

namespace Circuit.Elements.Active {
    class OpAmpUISwap : OpAmpUI {
        public OpAmpUISwap(Point pos) : base(pos) {
            DumpInfo.Flags |= FLAG_SWAP;
        }
    }
}
