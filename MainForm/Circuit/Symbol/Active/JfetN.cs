using Circuit.Elements.Active;

namespace Circuit.Symbol.Active {
	class JfetN : FET {
		protected JfetN(Point pos, bool isNch) : base(pos, isNch, false) { }

		public JfetN(Point pos) : base(pos, true, false) { }

		public override void Reset() {
			base.Reset();
			Element.V[ElmFET.VD_D1] = 0;
			Element.I[ElmFET.CUR_D1] = 0;
		}

		public override void Stamp() {
			base.Stamp();
			var elm = (ElmFET)Element;
			if (Element.Para[ElmFET.N_CH] < 0) {
				elm.D1_A = ElmFET.S;
				elm.D1_B = ElmFET.G;
			} else {
				elm.D1_A = ElmFET.G;
				elm.D1_B = ElmFET.S;
			}
			StampNonLinear(Element.Nodes[elm.D1_A]);
			StampNonLinear(Element.Nodes[elm.D1_B]);
		}
	}
}
