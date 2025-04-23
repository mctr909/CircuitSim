namespace Circuit.Elements.Passive {
	class ElmPot : BaseElement {
		public const int A = 0;
		public const int B = 1;
		public const int S = 2;

		public override int TermCount { get { return 3; } }

		protected override double GetCurrent(int n) { return -I[n]; }

		public override void SetVoltage(int n, double v) {
			V[n] = v;
			I[A] = (V[A] - V[S]) / Para[A];
			I[B] = (V[B] - V[S]) / Para[B];
			I[S] = -I[A] - I[B];
		}
	}
}
