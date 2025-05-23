﻿using Circuit.Elements;
using Circuit.Elements.Active;
using MainForm.Forms;

namespace Circuit.Symbol.Active {
	class DiodeZener : Diode {
		static string mLastZenerModelName = "default-zener";

		PointF[] mWing;

		public DiodeZener(Point pos) : base(pos, "Z") {
			ModelName = mLastZenerModelName;
			var model = DiodeModel.GetModelWithName(ModelName);
			Element.Para[ElmDiode.LEAKAGE] = model.SaturationCurrent;
			Element.Para[ElmDiode.V_SCALE] = model.VScale;
			Element.Para[ElmDiode.VD_COEF] = model.VdCoef;
			Element.Para[ElmDiode.V_ZENER] = model.BreakdownVoltage;
			Model = model;
			Setup();
		}

		public DiodeZener(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
			DiodeModel model;
			if ((f & FLAG_MODEL) == 0) {
				var vz = st.nextTokenDouble(5.6);
				ModelName = DiodeModel.GetModelWithParameters(Element.Para[ElmDiode.FW_DROP], vz).Name;
				model = DiodeModel.GetModelWithName(ModelName);
			} else {
				ModelName = mLastZenerModelName;
				model = DiodeModel.GetModelWithName(ModelName);
			}
			Element.Para[ElmDiode.LEAKAGE] = model.SaturationCurrent;
			Element.Para[ElmDiode.V_SCALE] = model.VScale;
			Element.Para[ElmDiode.VD_COEF] = model.VdCoef;
			Element.Para[ElmDiode.V_ZENER] = model.BreakdownVoltage;
			Model = model;
			Setup();
		}

		protected override BaseElement Create() {
			return new ElmDiodeZenner();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.ZENER; } }

		public override void SetPoints() {
			base.SetPoints();
			mCathode = new PointF[2];
			mWing = new PointF[2];
			var pa = new PointF[2];
			InterpolationLeadAB(ref pa[0], ref pa[1], -1.0 / BODY_LEN, HS);
			InterpolationLeadAB(ref mCathode[0], ref mCathode[1], 1, HS);
			InterpolationPoint(mCathode[0], mCathode[1], out mWing[0], -0.2, -HS);
			InterpolationPoint(mCathode[1], mCathode[0], out mWing[1], -0.2, -HS);
			mPoly = new PointF[] { pa[0], pa[1], mLead2 };
			SetTextPos();
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();

			/* draw arrow thingy */
			FillPolygon(mPoly);
			/* draw thing arrow is pointing to */
			DrawLine(mCathode[0], mCathode[1]);
			/* draw wings on cathode */
			DrawLine(mWing[0], mCathode[0]);
			DrawLine(mWing[1], mCathode[1]);

			DoDots();
			DrawName();
		}

		public override void GetInfo(string[] arr) {
			base.GetInfo(arr);
			arr[0] = "ツェナーダイオード";
			arr[3] = "降伏電圧：" + TextUtils.Voltage(Element.Para[ElmDiode.V_ZENER]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 2) {
				return new ElementInfo("降伏電圧", Element.Para[ElmDiode.V_ZENER]);
			}
			return base.GetElementInfo(r, c);
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			base.SetElementValue(n, c, ei);
		}
	}
}
