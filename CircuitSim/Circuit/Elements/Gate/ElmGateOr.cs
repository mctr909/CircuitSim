namespace Circuit.Elements.Gate {
	class ElmGateOr : ElmGate {
		public ElmGateOr() : base() { }

		public ElmGateOr(StringTokenizer st) : base(st) { }

		protected override bool calcFunction() {
			bool f = false;
			for (int i = 0; i != InputCount; i++) {
				f |= getInput(i);
			}
			return f;
		}
	}
}
