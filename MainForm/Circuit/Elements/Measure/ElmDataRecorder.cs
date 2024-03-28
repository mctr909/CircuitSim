namespace Circuit.Elements.Measure {
	class ElmDataRecorder : BaseElement {
		public int DataCount = 0;
		public int DataPtr = 0;
		public bool DataFull = false;
		public double[] Data;

		int mLastDataCount = 0;

		public override int TermCount { get { return 1; } }

		public override double voltage_diff() {
			return volts[0];
		}

		public override void reset() {
			DataPtr = 0;
			DataFull = false;
		}

		public override void prepare_iteration() {
			if (mLastDataCount != DataCount) {
				Data = new double[DataCount];
				DataPtr = 0;
				DataFull = false;
				mLastDataCount = DataCount;
			}
		}

		public override void finish_iteration() {
			if (DataPtr < DataCount) {
				Data[DataPtr++] = volts[0];
			} else {
				DataPtr = 0;
				DataFull = true;
			}
		}
	}
}
