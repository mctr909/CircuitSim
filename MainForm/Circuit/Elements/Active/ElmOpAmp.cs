namespace Circuit.Elements.Active {
	class ElmOpAmp : BaseElement {
		public const int N = 0;
		public const int P = 1;
		public const int O = 2;
		public const int LAST = 3;

		public const int GAIN = 0;
		public const int OUT_MAX = 1;
		public const int OUT_MIN = 2;

		private const double DX = 1e-4;

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff { get { return V[O] - V[P]; } }

		protected override void DoIteration() {
			var v = V[P] - V[N];
			var lastV = V[LAST];
			V[LAST] = v;

			var gain = Para[GAIN];
			v *= gain;

			var rnd = Random.Next(4) == 1;
			double clipMax = (v >= Para[OUT_MAX] && (lastV >= 0 || rnd)) ? 1 : 0;
			double clipMin = (v <= Para[OUT_MIN] && (lastV <= 0 || rnd)) ? 1 : 0;
			int clipped = (clipMax + clipMin) > 0 ? 1 : 0;
			clipMax *= Para[OUT_MAX];
			clipMin *= Para[OUT_MIN];

			var dv = 1.0 - DX / gain;
			v = dv * clipMax;
			v += dv * clipMin;
			v *= clipped;

			gain *= 1 - clipped;
			gain += DX * clipped;

			/* newton-raphson */
			var vsIndex = VOLTAGE_SOURCE_BEGIN + VoltSource;
			var row = NODE_INFOS[vsIndex].Row;
			var niN = NODE_INFOS[Nodes[N] - 1];
			var niP = NODE_INFOS[Nodes[P] - 1];
			var niO = NODE_INFOS[Nodes[O] - 1];
			MATRIX[row, niN.Col] += gain * niN.IsVariable;
			MATRIX[row, niP.Col] -= gain * niP.IsVariable;
			MATRIX[row, niO.Col] += niO.IsVariable;
			v -= gain * niN.Value;
			v += gain * niP.Value;
			v -= niO.Value;
			RIGHTSIDE[row] += v;
		}

		protected override double GetCurrent(int n) { return n == 2 ? -I[0] : 0; }
	}
}
