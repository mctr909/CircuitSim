namespace Circuit.Elements.Gate {
	class ElmGateAnd : ElmGate {
		public ElmGateAnd() : base() { }

		protected override bool CalcFunction() {
			bool f = true;
			for (int i = 0; i != InputCount; i++) {
				f &= GetInput(i);
			}
			return f;
		}
	}
}
