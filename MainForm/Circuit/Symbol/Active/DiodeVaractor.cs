using Circuit.Elements;
using Circuit.Elements.Active;
using MainForm.Forms;

namespace Circuit.Symbol.Active {
	class DiodeVaractor : Diode {
		PointF[] mPlate1;
		PointF[] mPlate2;

		public DiodeVaractor(Point pos) : base(pos, "Vc") {
			ModelName = LastModelName;
			var model = DiodeModel.GetModelWithName(ModelName);
			Element.Para[ElmDiode.LEAKAGE] = model.SaturationCurrent;
			Element.Para[ElmDiode.V_SCALE] = model.VScale;
			Element.Para[ElmDiode.VD_COEF] = model.VdCoef;
			Element.Para[ElmDiode.FW_DROP] = model.FwDrop;
			Model = model;
			Setup();
		}

		public DiodeVaractor(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
			st.nextToken(out ModelName, ModelName);
			var model = DiodeModel.GetModelWithName(ModelName);
			Element.V[ElmDiode.VD_CAP] = st.nextTokenDouble();
			Element.Para[ElmDiode.CAPACITANCE] = st.nextTokenDouble();
			Element.Para[ElmDiode.LEAKAGE] = model.SaturationCurrent;
			Element.Para[ElmDiode.V_SCALE] = model.VScale;
			Element.Para[ElmDiode.VD_COEF] = model.VdCoef;
			Element.Para[ElmDiode.FW_DROP] = model.FwDrop;
			Model = model;
			Setup();
		}

		protected override BaseElement Create() {
			return new ElmDiodeVaractor();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.VARACTOR; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override int InternalNodeCount { get { return 1; } }

		protected override void dump(List<object> optionList) {
			var ce = (ElmDiodeVaractor)Element;
			base.dump(optionList);
			optionList.Add(ce.V[ElmDiode.VD_CAP].ToString("g3"));
			optionList.Add(ce.Para[ElmDiode.CAPACITANCE].ToString("g3"));
		}

		public override void Reset() {
			base.Reset();
			Element.V[ElmDiode.VD_CAP] = 0;
		}

		public override void Stamp() {
			base.Stamp();
			StampVoltageSource(Element.Nodes[0], Element.Nodes[2], Element.VoltSource);
			StampNonLinear(Element.Nodes[2]);
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
			var ce = (ElmDiodeVaractor)Element;
			arr[0] = "可変容量ダイオード";
			arr[5] = "静電容量：" + TextUtils.Unit(ce.Para[ElmDiode.CAPACITANCE], "F");
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			var ce = (ElmDiodeVaractor)Element;
			if (c != 0) {
				return null;
			}
			if (r == 2) {
				return new ElementInfo("静電容量 @ 0V", ce.Para[ElmDiode.CAPACITANCE]);
			}
			return base.GetElementInfo(r, c);
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			var ce = (ElmDiodeVaractor)Element;
			if (n == 2) {
				ce.Para[ElmDiode.CAPACITANCE] = ei.Value;
				return;
			}
			base.SetElementValue(n, c, ei);
		}
	}
}
