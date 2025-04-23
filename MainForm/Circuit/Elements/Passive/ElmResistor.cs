namespace Circuit.Elements.Passive {
	class ElmResistor : BaseElement {
		public override void SetVoltage(int n, double v) {
			V[n] = v;
			I[0] = (V[0] - V[1]) / Para[0];
		}
	}
}
