﻿using Circuit.Elements.Logic;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Logic {
	class InvertingSchmitt : BaseSymbol {
		protected ElmInvertingSchmitt mElm;
		protected PointF[] gatePoly;
		protected PointF[] symbolPoly;

		PointF pcircle;
		double dlt;
		double dut;

		public override int VoltageSourceCount { get { return 1; } }
		// there is no current path through the InvertingSchmitt input, but there
		// is an indirect path through the output to ground.
		public override bool HasConnection(int n1, int n2) { return false; }
		public override bool HasGroundConnection(int nodeIndex) { return nodeIndex == 1; }

		protected InvertingSchmitt(Point p1, Point p2, int f) : base(p1, p2, f) {
			mElm = (ElmInvertingSchmitt)Element;
			Post.NoDiagonal = true;
		}

		public InvertingSchmitt(Point pos) : base(pos) {
			mElm = (ElmInvertingSchmitt)Element;
			Post.NoDiagonal = true;
		}

		public InvertingSchmitt(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = (ElmInvertingSchmitt)Element;
			mElm.SlewRate = st.nextTokenDouble(0.5);
			mElm.LowerTrigger = st.nextTokenDouble(1.66);
			mElm.UpperTrigger = st.nextTokenDouble(3.33);
			mElm.LogicOnLevel = st.nextTokenDouble(5);
			mElm.LogicOffLevel = st.nextTokenDouble(0);
			Post.NoDiagonal = true;
		}

		protected override BaseElement Create() {
			return new ElmInvertingSchmitt();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.INVERT_SCHMITT; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.SlewRate.ToString("g3"));
			optionList.Add(mElm.LowerTrigger);
			optionList.Add(mElm.UpperTrigger);
			optionList.Add(mElm.LogicOnLevel);
			optionList.Add(mElm.LogicOffLevel);
		}

		public override void Stamp() {
			StampVoltageSource(0, mElm.Nodes[1], mElm.VoltSource);
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			DrawPolygon(gatePoly);
			DrawPolygon(symbolPoly);
			DrawCircle(pcircle, 3);
			UpdateDotCount(mElm.I[0], ref mCurCount);
			DrawCurrentB(mCurCount);
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
			InterpolationPost(ref pcircle, 0.5 + (ww - 2) / Post.Len);

			gatePoly = new PointF[3];
			InterpolationLeadAB(ref gatePoly[0], ref gatePoly[1], 0, hs);
			InterpolationPost(ref gatePoly[2], 0.5 + (ww - 5) / Post.Len);

			CreateSchmitt(Post.A, Post.B, out symbolPoly, 0.8, .5 - (ww - 7) / Post.Len);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "inverting Schmitt trigger";
			arr[1] = "Vin：" + TextUtils.Voltage(mElm.V[0]);
			arr[2] = "Vout：" + TextUtils.Voltage(mElm.V[1]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				dlt = mElm.LowerTrigger;
				return new ElementInfo("Lower threshold (V)", mElm.LowerTrigger);
			}
			if (r == 1) {
				dut = mElm.UpperTrigger;
				return new ElementInfo("Upper threshold (V)", mElm.UpperTrigger);
			}
			if (r == 2) {
				return new ElementInfo("Slew Rate (V/ns)", mElm.SlewRate);
			}
			if (r == 3) {
				return new ElementInfo("High Voltage (V)", mElm.LogicOnLevel);
			}
			if (r == 4) {
				return new ElementInfo("Low Voltage (V)", mElm.LogicOffLevel);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				dlt = ei.Value;
			}
			if (n == 1) {
				dut = ei.Value;
			}
			if (n == 2) {
				mElm.SlewRate = ei.Value;
			}
			if (n == 3) {
				mElm.LogicOnLevel = ei.Value;
			}
			if (n == 4) {
				mElm.LogicOffLevel = ei.Value;
			}
			if (dlt > dut) {
				mElm.UpperTrigger = dlt;
				mElm.LowerTrigger = dut;
			} else {
				mElm.UpperTrigger = dut;
				mElm.LowerTrigger = dlt;
			}
		}
	}
}
