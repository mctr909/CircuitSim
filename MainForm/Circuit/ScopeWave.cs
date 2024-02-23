namespace Circuit {
	public class ScopeWave {
		public BaseSymbol Symbol;
		public int Color;
		public int Length;
		public int Speed;
		public int Index;
		public double[] MinValues;
		public double[] MaxValues;

		int mCounter;

		public ScopeWave(BaseSymbol symbol) {
			Symbol = symbol;
			Length = 1;
			Index = 0;
			MinValues = [Length];
			MaxValues = [Length];
		}

		public void Reset(int length, int speed, bool full) {
			var oldSpc = Length;
			Length = length;
			if (Speed != speed) {
				oldSpc = 0;
			}
			Speed = speed;
			var oldMin = MinValues;
			var oldMax = MaxValues;
			MinValues = new double[Length];
			MaxValues = new double[Length];
			if (oldMin != null && !full) {
				for (int i = 0; i != Length && i != oldSpc; i++) {
					var i1 = (-i) & (Length - 1);
					var i2 = (Index - i) & (oldSpc - 1);
					MinValues[i1] = oldMin[i2];
					MaxValues[i1] = oldMax[i2];
				}
			} else {
				mCounter = 0;
			}
			Index = 0;
		}

		public void TimeStep() {
			var v = Symbol.Element.VoltageDiff;
			if (v < MinValues[Index]) {
				MinValues[Index] = v;
			}
			if (v > MaxValues[Index]) {
				MaxValues[Index] = v;
			}
			mCounter++;
			if (mCounter >= Speed) {
				Index = (Index + 1) & (Length - 1);
				MinValues[Index] = MaxValues[Index] = v;
				mCounter = 0;
			}
		}
	}
}
