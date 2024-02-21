namespace Circuit.Elements.Logic {
	class ElmGateOr : ElmGate {
		public ElmGateOr() : base() { }

		protected override bool CalcFunction() {
			bool f = false;
			for (int i = 0; i != InputCount; i++) {
				f |= GetInput(i);
			}
			return f;
		}
	}
}
