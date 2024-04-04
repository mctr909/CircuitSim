namespace Circuit {
	public struct WAVE_VALUE {
		public float min;
		public float max;
	}

	public class SCOPE_WAVE {
		public int Length;
		public int Speed;
		public int Cursor;
		public int Interval;
		public int Color;
		public BaseSymbol Symbol;
		public BaseElement Elm;
		public WAVE_VALUE[] Data;
		public SCOPE_WAVE(BaseSymbol symbol, BaseElement element) {
			Length = 1;
			Symbol = symbol;
			Elm = element;
			Data = new WAVE_VALUE[Length];
		}
		public void Reset(int length, int speed, bool full) {
			var oldLength = Length;
			if (Speed != speed) {
				oldLength = 0;
			}
			Length = length;
			Speed = speed;
			if (full) {
				Data = new WAVE_VALUE[length];
				Interval = 0;
			} else {
				var old = new WAVE_VALUE[Data.Length];
				Array.Copy(Data, old, Data.Length);
				Data = new WAVE_VALUE[length];
				for (int i = 0; i != length && i != oldLength; i++) {
					var i1 = (-i) & (length - 1);
					var i2 = (Cursor - i) & (oldLength - 1);
					Data[i1].min = old[i2].min;
					Data[i1].max = old[i2].max;
				}
			}
			Cursor = 0;
		}
	}
}
