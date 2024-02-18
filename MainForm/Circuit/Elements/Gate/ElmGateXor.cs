namespace Circuit.Elements.Gate {
	class ElmGateXor : ElmGate {
		public ElmGateXor() : base() { }

		protected override bool CalcFunction() {
			bool f = false;
			for (int i = 0; i != InputCount; i++) {
				f ^= GetInput(i);
			}
			return f;
		}
	}
}
