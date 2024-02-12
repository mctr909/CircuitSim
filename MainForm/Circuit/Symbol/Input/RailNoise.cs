using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
	class RailNoise : Rail {
		public RailNoise(Point pos) : base(pos, ElmVoltage.WAVEFORM.NOISE) { }
	}
}
