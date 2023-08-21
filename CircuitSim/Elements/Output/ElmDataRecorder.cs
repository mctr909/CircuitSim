namespace Circuit.Elements.Output {
    class ElmDataRecorder : BaseElement {
        public double[] Data { get; private set; }
        public int DataCount { get; private set; }
        public int DataPtr { get; private set; }
        public bool DataFull { get; private set; }

        public ElmDataRecorder() : base() {
            setDataCount(10000);
        }

        public ElmDataRecorder(StringTokenizer st) : base() {
            var v = st.nextTokenInt();
            setDataCount(v);
        }

        public override int PostCount { get { return 1; } }

        public override double GetVoltageDiff() { return Volts[0]; }

        public override void CirIterationFinished() {
            Data[DataPtr++] = Volts[0];
            if (DataPtr >= DataCount) {
                DataPtr = 0;
                DataFull = true;
            }
        }

        public override void Reset() {
            DataPtr = 0;
            DataFull = false;
        }

        public void setDataCount(int ct) {
            DataCount = ct;
            DataPtr = 0;
            DataFull = false;
            Data = new double[DataCount];
        }
    }
}
