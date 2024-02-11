using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.Symbol.Active {
	class AnalogSwitch : BaseSymbol {
		const int FLAG_INVERT = 1;
		const int OPEN_HS = 16;
		const int BODY_LEN = 24;

		ElmAnalogSwitch mElm;

		PointF mCtrlTerm;
		PointF mCtrlLead;
		PointF mOpen;
		PointF mClose;

        public override BaseElement Element { get { return mElm; } }

        public AnalogSwitch(Point pos) : base(pos) {
			mElm = new ElmAnalogSwitch();
		}

		public AnalogSwitch(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
			mElm = new ElmAnalogSwitch();
			mElm.Ron = st.nextTokenDouble(1e-3);
			mElm.Roff = st.nextTokenDouble(1e9);
		}

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Ron.ToString("g3"));
			optionList.Add(mElm.Roff.ToString("g3"));
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.ANALOG_SW; } }

		public override void Drag(Point pos) {
			pos = CirSimForm.SnapGrid(pos);
			if (Math.Abs(Post.A.X - pos.X) < Math.Abs(Post.A.Y - pos.Y)) {
				pos.X = Post.A.X;
			} else {
				pos.Y = Post.A.Y;
			}
			int q1 = Math.Abs(Post.A.X - pos.X) + Math.Abs(Post.A.Y - pos.Y);
			int q2 = (q1 / 2) % CirSimForm.GRID_SIZE;
			if (q2 != 0) {
				return;
			}
			Post.B = pos;
			SetPoints();
		}

		public override void SetPoints() {
			base.SetPoints();
			setLeads(BODY_LEN);
			interpPost(ref mCtrlTerm, 0.5, -OPEN_HS);
			interpPost(ref mCtrlLead, 0.5, -OPEN_HS / 3);
			interpLead(ref mOpen, 1 - 2.0 / BODY_LEN, OPEN_HS - 6);
			interpLead(ref mClose, 1 - 2.0 / BODY_LEN, 2.5f);
			mElm.SetNodePos(Post.A, Post.B, mCtrlTerm);
		}

		public override void Draw(CustomGraphics g) {
			draw2Leads();
			drawLine(mCtrlTerm, mCtrlLead);
			fillCircle(mLead1, 2.5f);
			fillCircle(mLead2, 2.5f);
			if (mElm.IsOpen || g is PDF.Page) {
				drawLine(mLead1, mOpen);
			} else {
				drawLine(mLead1, mClose);
			}
			doDots();
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "アナログスイッチ(" + (mElm.IsOpen ? "OFF)" : "ON)");
			arr[1] = "電位差：" + Utils.VoltageAbsText(mElm.VoltageDiff);
			arr[2] = "電流：" + Utils.CurrentAbsText(mElm.Current);
			arr[3] = "制御電圧：" + Utils.VoltageText(mElm.Volts[2]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("ノーマリクローズ", (mFlags & FLAG_INVERT) != 0);
			}
			if (r == 1) {
				return new ElementInfo("オン抵抗(Ω)", mElm.Ron);
			}
			if (r == 2) {
				return new ElementInfo("オフ抵抗(Ω)", mElm.Roff);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mFlags = ei.CheckBox.Checked ? (mFlags | FLAG_INVERT) : (mFlags & ~FLAG_INVERT);
				mElm.Invert = 0 != (mFlags & FLAG_INVERT);
			}
			if (n == 1 && 0 < ei.Value) {
				mElm.Ron = ei.Value;
			}
			if (n == 2 && 0 < ei.Value) {
				mElm.Roff = ei.Value;
			}
		}
	}
}
