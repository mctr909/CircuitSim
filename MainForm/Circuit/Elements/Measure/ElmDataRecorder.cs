namespace Circuit.Elements.Measure {
	class ElmDataRecorder : BaseElement {
		public int DataCount = 0;
		public int DataPtr = 0;
		public bool DataFull = false;
		public double[] Data;

		int mLastDataCount = 0;

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return V[0]; } }

		protected override void StartIteration() {
			if (mLastDataCount != DataCount) {
				Data = new double[DataCount];
				DataPtr = 0;
				DataFull = false;
				mLastDataCount = DataCount;
			}
		}

		protected override void FinishIteration() {
			if (DataPtr < DataCount) {
				Data[DataPtr++] = V[0];
			} else {
				DataPtr = 0;
				DataFull = true;
			}
		}
	}
}
