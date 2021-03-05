namespace Circuit.Elements {
    class DCVoltageElm : VoltageElm {
        public DCVoltageElm(int xx, int yy) : base(xx, yy, WAVEFORM.DC) { }
    }
}
