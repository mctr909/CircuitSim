using Circuit.Symbol;

namespace Circuit.Elements {
	public struct WAVE_VALUE {
		public float Min;
		public float Max;
	}

	public class SCOPE_WAVE {
		public int Length;
		public int Speed;
		public int Interval;
		public int Cursor;
		public int Color;
		public BaseSymbol Symbol;
		public BaseElement Elm;
		public WAVE_VALUE[] Values;

		public SCOPE_WAVE(BaseSymbol symbol, BaseElement element) {
			Symbol = symbol;
			Elm = element;
			Length = 1;
			Values = new WAVE_VALUE[Length];
		}
	}
}
