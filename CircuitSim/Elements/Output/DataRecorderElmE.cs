namespace Circuit.Elements.Output {
    class DataRecorderElmE : BaseElement {
        public double[] Data { get; private set; }
        public int DataCount { get; private set; }
        public int DataPtr { get; private set; }
        public bool DataFull { get; private set; }

        public DataRecorderElmE() : base() {
            setDataCount(10000);
        }

        public DataRecorderElmE(StringTokenizer st) : base() {
            setDataCount(int.Parse(st.nextToken()));
        }

        public override int CirPostCount { get { return 1; } }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override void CirStepFinished() {
            Data[DataPtr++] = CirVolts[0];
            if (DataPtr >= DataCount) {
                DataPtr = 0;
                DataFull = true;
            }
        }

        public override void CirReset() {
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
