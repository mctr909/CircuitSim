﻿using Circuit.Elements.Logic;
using Circuit.Elements;

namespace Circuit.Symbol.Logic {
	class GateOr : Gate {
		protected GateOr(Point pos, bool dummy) : base(pos) { }
		protected GateOr(Point p1, Point p2, int f, bool dummy) : base(p1, p2, f) { }

		public GateOr(Point pos) : base(pos) {
			mElm = (ElmGateOr)Element;
		}

		public GateOr(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = (ElmGateOr)Element;
			mElm.InputCount = st.nextTokenInt(mElm.InputCount);
			var lastOutputVoltage = st.nextTokenDouble();
			mElm.HighVoltage = st.nextTokenDouble(5);
			mElm.LastOutput = mElm.HighVoltage * 0.5 < lastOutputVoltage;
		}

		protected override BaseElement Create() {
			return new ElmGateOr();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.OR_GATE; } }

		protected override string gateName { get { return "OR gate"; } }

		protected override string gateText { get { return "\u22651"; } }

		public override void SetPoints() {
			base.SetPoints();
			CreateEuroGatePolygon();

			/* 0-15  = top curve,
             * 16    = right,
             * 17-32 = bottom curve,
             * 33-39 = left curve */
			mGatePolyAnsi = new PointF[40];
			if (this is GateXor) {
				mLinePoints = new PointF[7];
			}
			for (int i = 0; i != 16; i++) {
				var a = i / 16.0;
				var b = 1 - a * a;
				InterpolationLeadAB(ref mGatePolyAnsi[i], ref mGatePolyAnsi[32 - i], 0.5 + a / 2, b * mHs2);
			}
			var ww2 = (mWw == 0) ? Post.Len * 2 : mWw * 2;
			for (int i = 0; i != 7; i++) {
				var a = (i - 3) / 3.0;
				var b = 6 * (1 - a * a) - 3;
				InterpolationLead(ref mGatePolyAnsi[33 + i], b / ww2, a * mHs2);
				if (this is GateXor) {
					InterpolationLead(ref mLinePoints[i], (b - 5) / ww2, a * mHs2);
				}
			}
			mGatePolyAnsi[16] = mLead2;

			if (mElm.IsInverting) {
				InterpolationPost(ref mCirclePos, 0.5 + (mWw + 3) / Post.Len);
				SetLead2(0.5 + (mWw + 6) / Post.Len);
			}
		}
	}
}
