using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
	class Inductor : BaseSymbol {
		protected static string mLastReferenceName = "L";
		protected static double mLastValue = 1e-4;

		const int BODY_LEN = 24;
		const int COIL_WIDTH = 8;

		ElmInductor mElm;
		PointF[] mCoilPos;
		float mCoilAngle;

		public override BaseElement Element { get { return mElm; } }

		public Inductor(Point pos) : base(pos) {
			mElm = new ElmInductor();
			ReferenceName = mLastReferenceName;
			mElm.Inductance = mLastValue;
		}

		public Inductor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			var ind = st.nextTokenDouble(mLastValue);
			var c = st.nextTokenDouble(0);
			mElm = new ElmInductor(ind, c);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.INDUCTOR; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Inductance.ToString("g3"));
			optionList.Add(mElm.Current.ToString("g3"));
		}

		public override void SetPoints() {
			base.SetPoints();
			setLeads(BODY_LEN);
			SetCoilPos(mLead1, mLead2);
			SetTextPos();
		}

		void SetCoilPos(PointF a, PointF b) {
			var coilLen = (float)Utils.Distance(a, b);
			var loopCt = (int)Math.Ceiling(coilLen / 11);
			var arr = new List<PointF>();
			for (int loop = 0; loop != loopCt; loop++) {
				PointF p;
				Utils.InterpPoint(a, b, out p, (loop + 0.5) / loopCt, 0);
				arr.Add(p);
			}
			mCoilPos = arr.ToArray();
			mCoilAngle = (float)(Utils.Angle(a, b) * 180 / Math.PI);
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
			if (0 < deg && deg < 45 * 3) {
				interpPost(ref mValuePos, 0.5, 9 * Post.Dsign);
				interpPost(ref mNamePos, 0.5, -9 * Post.Dsign);
			} else if (45 * 3 <= deg && deg <= 180) {
				interpPost(ref mNamePos, 0.5, 7 * Post.Dsign);
				interpPost(ref mValuePos, 0.5, -13 * Post.Dsign);
			} else if (180 < deg && deg < 45 * 7) {
				interpPost(ref mNamePos, 0.5, -7 * Post.Dsign);
				interpPost(ref mValuePos, 0.5, 13 * Post.Dsign);
			} else {
				interpPost(ref mNamePos, 0.5, 11 * Post.Dsign);
				interpPost(ref mValuePos, 0.5, -9 * Post.Dsign);
			}
		}

		public override void Draw(CustomGraphics g) {
			draw2Leads();
			foreach (var p in mCoilPos) {
				drawArc(p, COIL_WIDTH, mCoilAngle, -180);
			}
			drawName();
			drawValue(Utils.UnitText(mElm.Inductance));
			doDots();
		}

		public override void GetInfo(string[] arr) {
			if (string.IsNullOrEmpty(ReferenceName)) {
				arr[0] = "コイル：" + Utils.UnitText(mElm.Inductance, "H");
				getBasicInfo(1, arr);
			} else {
				arr[0] = ReferenceName;
				arr[1] = "コイル：" + Utils.UnitText(mElm.Inductance, "H");
				getBasicInfo(2, arr);
			}
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("インダクタンス(H)", mElm.Inductance);
			}
			if (r == 1) {
				return new ElementInfo("名前", ReferenceName);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0 && ei.Value > 0) {
				mElm.Inductance = ei.Value;
				mLastValue = ei.Value;
				SetTextPos();
			}
			if (n == 1) {
				ReferenceName = ei.Text;
				mLastReferenceName = ReferenceName;
				SetTextPos();
			}
			mElm.Setup(mElm.Inductance, mElm.Current);
		}

		public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
			return new EventHandler((s, e) => {
				var trb = adj.Slider;
				mElm.Inductance = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
				CirSimForm.NeedAnalyze();
			});
		}
	}
}
