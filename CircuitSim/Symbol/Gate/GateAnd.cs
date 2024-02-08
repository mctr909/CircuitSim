﻿using System;
using System.Drawing;

using Circuit.Elements.Gate;

namespace Circuit.Symbol.Gate {
	class GateAnd : Gate {
		public GateAnd(Point pos) : base(pos) {
			mElm = new ElmGateAnd();
			Elm = mElm;
		}

		public GateAnd(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
			mElm = new ElmGateAnd(st);
			Elm = mElm;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.AND_GATE; } }

		protected override string gateText { get { return "&"; } }

		protected override string gateName { get { return "AND gate"; } }

		public override void SetPoints() {
			base.SetPoints();

			CreateEuroGatePolygon();

			/* 0    = topleft,
             * 1-10 = top curve,
             * 11   = right,
             * 12-21= bottom curve,
             * 22   = bottom left */
			mGatePolyAnsi = new PointF[23];
			interpLeadAB(ref mGatePolyAnsi[0], ref mGatePolyAnsi[22], 0, mHs2);
			for (int i = 0; i != 10; i++) {
				double a = i * .1;
				double b = Math.Sqrt(1 - a * a);
				interpLeadAB(ref mGatePolyAnsi[i + 1], ref mGatePolyAnsi[21 - i], 0.5 + a / 2, b * mHs2);
			}
			mGatePolyAnsi[11] = mLead2;

			if (mElm.IsInverting) {
				interpPost(ref mCirclePos, 0.5 + (mWw + 3) / Post.Len);
				setLead2(0.5 + (mWw + 6) / Post.Len);
			}
		}
	}
}