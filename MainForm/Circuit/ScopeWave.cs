namespace Circuit {
	public struct WAVE_VALUE {
		public float min;
		public float max;
	}

	public class SCOPE_WAVE {
		public int length;
		public int speed;
		public int index;
		public int counter;
		public int color;
		public BaseSymbol p_symbol;
		public BaseElement p_elm;
		public WAVE_VALUE[] p_values;

		public SCOPE_WAVE(BaseSymbol symbol, BaseElement element) {
			length = 1;
			p_symbol = symbol;
			p_elm = element;
			p_values = new WAVE_VALUE[length];
		}

		public void reset(int length, int speed, bool full) {
			var oldSpc = this.length;
			this.length = length;
			if (this.speed != speed) {
				oldSpc = 0;
			}
			this.speed = speed;
			if (full) {
				p_values = new WAVE_VALUE[this.length];
				counter = 0;
			} else {
				var old = new WAVE_VALUE[p_values.Length];
				Array.Copy(p_values, old, p_values.Length);
				p_values = new WAVE_VALUE[this.length];
				for (int i = 0; i != this.length && i != oldSpc; i++) {
					var i1 = (-i) & (this.length - 1);
					var i2 = (index - i) & (oldSpc - 1);
					p_values[i1].min = old[i2].min;
					p_values[i1].max = old[i2].max;
				}
			}
			index = 0;
		}
	}
}
