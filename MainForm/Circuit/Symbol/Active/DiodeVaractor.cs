using Circuit.Forms;
using Circuit.Elements.Active;

namespace Circuit.Symbol.Active {
	class DiodeVaractor : Diode {
		PointF[] mPlate1;
		PointF[] mPlate2;

		public DiodeVaractor(Point pos) : base(pos, "Vc") {
			mElm = new ElmDiodeVaractor();
			mElm.Setup();
		}

		public DiodeVaractor(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
			mElm = new ElmDiodeVaractor(st);
			mElm.Setup();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.VARACTOR; } }

		protected override void dump(List<object> optionList) {
			var ce = (ElmDiodeVaractor)mElm;
			base.dump(optionList);
			optionList.Add(ce.mCapVoltDiff.ToString("g3"));
			optionList.Add(ce.BaseCapacitance.ToString("g3"));
		}

		public override void SetPoints() {
			BODY_LEN = 12;
			base.SetPoints();
			var plate11 = 1.0 - 3.0 / BODY_LEN;
			var plate12 = 1.0 - 5.0 / BODY_LEN;
			var plate2 = 1.0 - 1.0 / BODY_LEN;
			var pa = new PointF[2];
			InterpolationLeadAB(ref pa[0], ref pa[1], -1.0 / BODY_LEN, HS);
			var arrowPoint = new PointF();
			InterpolationLead(ref arrowPoint, plate11);
			mPoly = new PointF[] { pa[0], pa[1], arrowPoint };
			// calc plates
			mPlate1 = new PointF[4];
			mPlate2 = new PointF[4];
			InterpolationLeadAB(ref mPlate1[0], ref mPlate1[1], plate11, HS);
			InterpolationLeadAB(ref mPlate1[3], ref mPlate1[2], plate12, HS);
			InterpolationLeadAB(ref mPlate2[0], ref mPlate2[1], plate2, HS);
			InterpolationLeadAB(ref mPlate2[3], ref mPlate2[2], 1, HS);
			SetTextPos();
		}

		public override void Draw(CustomGraphics g) {
			// draw leads and diode arrow
			DrawDiode();
			// draw first plate
			FillPolygon(mPlate1);
			// draw second plate
			FillPolygon(mPlate2);
			DoDots();
			DrawName();
		}

		public override void GetInfo(string[] arr) {
			base.GetInfo(arr);
			var ce = (ElmDiodeVaractor)mElm;
			arr[0] = "可変容量ダイオード";
			arr[5] = "静電容量：" + TextUtils.Unit(ce.Capacitance, "F");
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			var ce = (ElmDiodeVaractor)mElm;
			if (c != 0) {
				return null;
			}
			if (r == 2) {
				return new ElementInfo("静電容量 @ 0V", ce.BaseCapacitance);
			}
			return base.GetElementInfo(r, c);
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			var ce = (ElmDiodeVaractor)mElm;
			if (n == 2) {
				ce.BaseCapacitance = ei.Value;
				return;
			}
			base.SetElementValue(n, c, ei);
		}
	}
}
