﻿using System.Drawing;

namespace Circuit.Symbol.Active {
	class DiodeZener : Diode {
		static string mLastZenerModelName = "default-zener";

		PointF[] mWing;

		public DiodeZener(Point pos) : base(pos, "Z") {
			mElm.ModelName = mLastZenerModelName;
			mElm.Setup();
		}

		public DiodeZener(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
			if ((f & FLAG_MODEL) == 0) {
				var vz = st.nextTokenDouble(5.6);
				mElm.Model = DiodeModel.GetModelWithParameters(mElm.Model.FwDrop, vz);
				mElm.ModelName = mElm.Model.Name;
			}
			mElm.Setup();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.ZENER; } }

		public override void SetPoints() {
			base.SetPoints();
			mCathode = new PointF[2];
			mWing = new PointF[2];
			var pa = new PointF[2];
			interpLeadAB(ref pa[0], ref pa[1], -1.0 / BODY_LEN, HS);
			interpLeadAB(ref mCathode[0], ref mCathode[1], 1, HS);
			Utils.InterpPoint(mCathode[0], mCathode[1], out mWing[0], -0.2, -HS);
			Utils.InterpPoint(mCathode[1], mCathode[0], out mWing[1], -0.2, -HS);
			mPoly = new PointF[] { pa[0], pa[1], mLead2 };
			SetTextPos();
		}

		public override void Draw(CustomGraphics g) {
			draw2Leads();

			/* draw arrow thingy */
			fillPolygon(mPoly);
			/* draw thing arrow is pointing to */
			drawLine(mCathode[0], mCathode[1]);
			/* draw wings on cathode */
			drawLine(mWing[0], mCathode[0]);
			drawLine(mWing[1], mCathode[1]);

			doDots();
			drawName();
		}

		public override void GetInfo(string[] arr) {
			base.GetInfo(arr);
			arr[0] = "ツェナーダイオード";
			arr[3] = "降伏電圧：" + Utils.VoltageText(mElm.Model.BreakdownVoltage);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 2) {
				return new ElementInfo("降伏電圧", mElm.Model.BreakdownVoltage);
			}
			return base.GetElementInfo(r, c);
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			base.SetElementValue(n, c, ei);
		}
	}
}