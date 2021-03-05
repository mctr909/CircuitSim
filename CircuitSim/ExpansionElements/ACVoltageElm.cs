namespace Circuit.Elements {
    class ACVoltageElm : VoltageElm {
        public ACVoltageElm(int xx, int yy) : base(xx, yy, WAVEFORM.AC) { }
    }
}
