using Circuit.Elements.Input;
using Circuit.Elements.Custom;

namespace Circuit.Elements.Active {
	class ElmOptocoupler : ElmComposite {
		public ElmDiode Diode;
		public ElmTransistor Transistor;

		protected override void Init(string expr) {
			Diode = (ElmDiode)CompList[0];

			var cccs = (ElmCCCS)CompList[1];
			cccs.SetExpr(expr);

			Transistor = (ElmTransistor)CompList[2];
			Transistor.SetHfe(700);
		}

		public override void Reset() {
			base.Reset();
		}

		public override bool GetConnection(int n1, int n2) {
			return n1 / 2 == n2 / 2;
		}
	}
}
