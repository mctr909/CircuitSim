namespace Circuit.Elements {
    class OpAmpSwapElm : OpAmpElm {
        public OpAmpSwapElm(int xx, int yy) : base(xx, yy) {
            mFlags |= FLAG_SWAP;
        }
    }
}
