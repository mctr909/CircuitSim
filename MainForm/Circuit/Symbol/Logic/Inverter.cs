﻿using Circuit.Elements.Logic;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Logic {
	class Inverter : BaseSymbol {
		ElmInverter mElm;
		PointF[] mGatePolyEuro;
		PointF[] mGatePolyAnsi;
		PointF mCenter;
		PointF mPcircle;

		public override int VoltageSourceCount { get { return 1; } }
		/* there is no current path through the inverter input,
         * but there is an indirect path through the output to ground. */
		public override bool HasConnection(int n1, int n2) { return false; }
		public override bool HasGroundConnection(int nodeIndex) { return nodeIndex == 1; }

		public Inverter(Point pos) : base(pos) {
			mElm = (ElmInverter)Element;
			Post.NoDiagonal = true;
		}

		public Inverter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = (ElmInverter)Element;
			mElm.SlewRate = st.nextTokenDouble(0.5);
			mElm.HighVoltage = st.nextTokenDouble(5);
			Post.NoDiagonal = true;
		}

		protected override BaseElement Create() {
			return new ElmInverter();
		}

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.SlewRate.ToString("g3"));
			optionList.Add(mElm.HighVoltage);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.INVERT; } }

		public override void Stamp() {
			StampVoltageSource(0, mElm.Nodes[1], mElm.VoltSource);
		}

		public override void SetPoints() {
			base.SetPoints();
			int hs = 10;
			int ww = 12;
			if (ww > Post.Len / 2) {
				ww = (int)(Post.Len / 2);
			}
			SetLead1(0.5 - ww / Post.Len);
			SetLead2(0.5 + (ww + 2) / Post.Len);
			InterpolationPost(ref mPcircle, 0.5 + (ww - 2) / Post.Len);

			mGatePolyAnsi = new PointF[3];
			InterpolationLeadAB(ref mGatePolyAnsi[0], ref mGatePolyAnsi[1], 0, hs);
			InterpolationPost(ref mGatePolyAnsi[2], 0.5 + (ww - 5) / Post.Len);

			mGatePolyEuro = new PointF[4];
			var l2 = new PointF();
			InterpolationPost(ref l2, 0.5 + (ww - 5) / Post.Len); /* make room for circle */
			InterpolationPoint(mLead1, l2, out mGatePolyEuro[0], out mGatePolyEuro[1], 0, hs);
			InterpolationPoint(mLead1, l2, out mGatePolyEuro[3], out mGatePolyEuro[2], 1, hs);
			InterpolationPoint(mLead1, l2, out mCenter, .5);
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			if (Gate.UseAnsiGates()) {
				DrawPolygon(mGatePolyAnsi);
			} else {
				DrawPolygon(mGatePolyEuro);
				DrawCenteredLText("1", mCenter);
			}
			DrawCircle(mPcircle, 3);
			UpdateDotCount(mElm.I[0], ref mCurCount);
			DrawCurrentB(mCurCount);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "inverter";
			arr[1] = "Vin：" + TextUtils.Voltage(mElm.V[0]);
			arr[2] = "Vout：" + TextUtils.Voltage(mElm.V[1]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("Slew Rate (V/ns)", mElm.SlewRate);
			}
			if (r == 1) {
				return new ElementInfo("High電圧", mElm.HighVoltage);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.SlewRate = ei.Value;
			}
			if (n == 1) {
				mElm.HighVoltage = ElmGate.LastHighVoltage = ei.Value;
			}
		}
	}
}
