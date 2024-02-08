﻿using System.Drawing;
using System.Collections.Generic;

using Circuit.Elements.Gate;

namespace Circuit.Symbol.Gate {
	class Inverter : BaseSymbol {
		ElmInverter mElm;
		PointF[] mGatePolyEuro;
		PointF[] mGatePolyAnsi;
		PointF mCenter;
		PointF mPcircle;

		public Inverter(Point pos) : base(pos) {
			mElm = new ElmInverter();
			Elm = mElm;
			Post.NoDiagonal = true;
		}

		public Inverter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmInverter(st);
			Elm = mElm;
			Post.NoDiagonal = true;
		}

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.SlewRate.ToString("g3"));
			optionList.Add(mElm.HighVoltage);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.INVERT; } }

		public override void SetPoints() {
			base.SetPoints();
			int hs = 10;
			int ww = 12;
			if (ww > Post.Len / 2) {
				ww = (int)(Post.Len / 2);
			}
			setLead1(0.5 - ww / Post.Len);
			setLead2(0.5 + (ww + 2) / Post.Len);
			interpPost(ref mPcircle, 0.5 + (ww - 2) / Post.Len);

			mGatePolyAnsi = new PointF[3];
			interpLeadAB(ref mGatePolyAnsi[0], ref mGatePolyAnsi[1], 0, hs);
			interpPost(ref mGatePolyAnsi[2], 0.5 + (ww - 5) / Post.Len);

			mGatePolyEuro = new PointF[4];
			var l2 = new PointF();
			interpPost(ref l2, 0.5 + (ww - 5) / Post.Len); /* make room for circle */
			Utils.InterpPoint(mLead1, l2, out mGatePolyEuro[0], out mGatePolyEuro[1], 0, hs);
			Utils.InterpPoint(mLead1, l2, out mGatePolyEuro[3], out mGatePolyEuro[2], 1, hs);
			Utils.InterpPoint(mLead1, l2, out mCenter, .5);
		}

		public override void Draw(CustomGraphics g) {
			draw2Leads();
			if (Gate.UseAnsiGates()) {
				drawPolygon(mGatePolyAnsi);
			} else {
				drawPolygon(mGatePolyEuro);
				drawCenteredLText("1", mCenter, true);
			}
			drawCircle(mPcircle, 3);
			updateDotCount(mElm.Current, ref mCurCount);
			drawCurrentB(mCurCount);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "inverter";
			arr[1] = "Vin：" + Utils.VoltageText(mElm.Volts[0]);
			arr[2] = "Vout：" + Utils.VoltageText(mElm.Volts[1]);
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