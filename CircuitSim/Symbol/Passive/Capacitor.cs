﻿using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
	class Capacitor : BaseSymbol {
		public static readonly int FLAG_BACK_EULER = 2;
		protected static string mLastReferenceName = "C";
		protected static double mLastValue = 1e-5;

		const int BODY_LEN = 5;
		const int HS = 6;

		ElmCapacitor mElm;
		PointF[] mPlate1;
		PointF[] mPlate2;

		public Capacitor(Point pos) : base(pos) {
			mElm = new ElmCapacitor();
			Elm = mElm;
			mElm.Capacitance = mLastValue;
			ReferenceName = mLastReferenceName;
		}

		public Capacitor(Point p1, Point p2, int f) : base(p1, p2, f) {
			ReferenceName = mLastReferenceName;
		}

		public Capacitor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmCapacitor();
			Elm = mElm;
			mElm.Capacitance = st.nextTokenDouble(mLastValue);
			mElm.VoltDiff = st.nextTokenDouble(0);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.CAPACITOR; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Capacitance.ToString("g3"));
			optionList.Add(mElm.VoltDiff.ToString("g3"));
		}

		public override void SetPoints() {
			base.SetPoints();
			/* calc leads */
			setLeads(BODY_LEN);
			/* calc plates */
			var dw = 0.8 / Post.Len;
			var f1 = 0.5 - BODY_LEN * 0.5 / Post.Len;
			var f2 = 0.5 + BODY_LEN * 0.5 / Post.Len;
			mPlate1 = new PointF[4];
			interpPost(ref mPlate1[0], f1 - dw, -HS);
			interpPost(ref mPlate1[1], f1 - dw, HS);
			interpPost(ref mPlate1[2], f1 + dw, HS);
			interpPost(ref mPlate1[3], f1 + dw, -HS);
			mPlate2 = new PointF[4];
			interpPost(ref mPlate2[0], f2 - dw, -HS);
			interpPost(ref mPlate2[1], f2 - dw, HS);
			interpPost(ref mPlate2[2], f2 + dw, HS);
			interpPost(ref mPlate2[3], f2 + dw, -HS);
			SetTextPos();
		}

		void SetTextPos() {
			var abX = Post.B.X - Post.A.X;
			var abY = Post.B.Y - Post.A.Y;
			mTextRot = Math.Atan2(abY, abX);
			var deg = -mTextRot * 180 / Math.PI;
			if (deg < 0.0) {
				deg += 360;
			}
			if (45 * 3 <= deg && deg < 45 * 7) {
				mTextRot += Math.PI;
			}
			int on, ov;
			if (0 == deg) {
				on = 12;
				ov = -11;
			} else if (0 < deg && deg < 45 * 3) {
				on = 10;
				ov = -13;
			} else if (45 * 3 <= deg && deg < 180) {
				on = -10;
				ov = 14;
			} else if (180 <= deg && deg < 45 * 7) {
				on = -10;
				ov = 13;
			} else {
				on = 11;
				ov = -12;
			}
			interpPost(ref mNamePos, 0.5, on);
			interpPost(ref mValuePos, 0.5, ov);
		}

		public override void Draw(CustomGraphics g) {
			draw2Leads();

			/* draw first lead and plate */
			fillPolygon(mPlate1);
			/* draw second lead and plate */
			fillPolygon(mPlate2);

			drawName();
			drawValue(Utils.UnitText(mElm.Capacitance));

			updateDotCount();
			if (CirSimForm.ConstructElm != this) {
				drawCurrentA(mCurCount);
				drawCurrentB(mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			if (string.IsNullOrEmpty(ReferenceName)) {
				arr[0] = "コンデンサ：" + Utils.UnitText(mElm.Capacitance, "F");
				getBasicInfo(1, arr);
			} else {
				arr[0] = ReferenceName;
				arr[1] = "コンデンサ：" + Utils.UnitText(mElm.Capacitance, "F");
				getBasicInfo(2, arr);
			}
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("キャパシタンス(F)", mElm.Capacitance);
			}
			if (r == 1) {
				return new ElementInfo("名前", ReferenceName);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0 && ei.Value > 0) {
				mElm.Capacitance = ei.Value;
				mLastValue = ei.Value;
				SetTextPos();
			}
			if (n == 1) {
				ReferenceName = ei.Text;
				mLastReferenceName = ReferenceName;
				SetTextPos();
			}
		}

		public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
			return new EventHandler((s, e) => {
				var trb = adj.Slider;
				mElm.Capacitance = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
				CirSimForm.NeedAnalyze();
			});
		}
	}
}