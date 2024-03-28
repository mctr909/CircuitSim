using Circuit.Elements.Custom;
using Circuit.Elements.Input;

namespace Circuit.Elements.Active {
	class ElmOptocoupler : ElmComposite {
		protected override void Init() {
			((ElmCCCS)CompList[1]).SetFunction((inputs) => {
				var i = inputs[0];
				double v;
				if (i < 0.003) {
					v = (-80000000000 * i * i * i * i * i
						+ 800000000 * i * i * i * i
						- 3000000 * i * i * i
						+ 5177.20 * i * i
						+ 0.2453 * i
						- 0.00005
					) * 1.040 / 700;
				} else {
					v = (9000000 * i * i * i * i * i
						- 998113 * i * i * i * i
						+ 42174 * i * i * i
						- 861.32 * i * i
						+ 9.0836 * i
						- 0.00780
					) * 0.945 / 700;
				}
				return Math.Max(0, Math.Min(0.0001, v));
			});
			((ElmTransistor)CompList[2]).SetHfe(700);
		}

		public override bool has_connection(int n1, int n2) {
			return n1 / 2 == n2 / 2;
		}
	}
}
