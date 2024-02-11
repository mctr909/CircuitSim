using Circuit.Elements.Custom;
using Circuit.Elements.Input;
using System;
using System.Collections.Generic;

namespace Circuit.Elements.Active {
	class ElmOptocoupler : ElmComposite {
		static readonly int[] EXTERNAL_NODES = { 6, 2, 4, 5 };
		static readonly string MODEL_STRING
			= DUMP_ID.DIODE + " 6 1\r"
			+ DUMP_ID.CCCS + " 1 2 3 4\r"
			+ DUMP_ID.TRANSISTOR_N + " 3 4 5";

		public ElmDiode Diode;
		public ElmTransistor Transistor;

		public ElmOptocoupler(List<BaseSymbol> symbolList) {
			LoadComposite(symbolList, MODEL_STRING, EXTERNAL_NODES);
		}

		protected override void Init() {
			Diode = (ElmDiode)CompList[0];
			var cccs = (ElmCCCS)CompList[1];
			cccs.SetFunction((inputs) => {
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
