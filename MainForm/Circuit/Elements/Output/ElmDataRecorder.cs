namespace Circuit.Elements.Output {
	class ElmDataRecorder : BaseElement {
		public int DataCount = 0;
		public int DataPtr = 0;
		public bool DataFull = false;
		public double[] Data;

		int mLastDataCount = 0;

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

        public override void PrepareIteration() {
			if (mLastDataCount != DataCount) {
				Data = new double[DataCount];
				DataPtr = 0;
				DataFull = false;
				mLastDataCount = DataCount;
			}
        }

        public override void IterationFinished() {
			if (DataPtr < DataCount) {
				Data[DataPtr++] = Volts[0];
			} else {
				DataPtr = 0;
				DataFull = true;
			}
		}

		public override void Reset() {
			DataPtr = 0;
			DataFull = false;
		}
	}
}
