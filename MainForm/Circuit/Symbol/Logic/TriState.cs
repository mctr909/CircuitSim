﻿using Circuit.Forms;
using Circuit.Elements.Logic;

namespace Circuit.Symbol.Logic {
	class TriState : BaseSymbol {
		const int BODY_LEN = 16;

		ElmTriState mElm;
		PointF mCtrlLead;
		PointF mCtrlTerm;
		PointF[] mGatePoly;

		public override BaseElement Element { get { return mElm; } }

		public TriState(Point pos) : base(pos) {
			mElm = new ElmTriState();
		}

		public TriState(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmTriState();
			mElm.Ron = st.nextTokenDouble(0.1);
			mElm.Roff = st.nextTokenDouble(1e10);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.TRISTATE; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Ron.ToString("g3"));
			optionList.Add(mElm.Roff.ToString("g3"));
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLeads(BODY_LEN);
			int hs = BODY_LEN / 2;
			int ww = BODY_LEN / 2;
			if (ww > Post.Len / 2) {
				ww = (int)(Post.Len / 2);
			}
			mGatePoly = new PointF[3];
			InterpolationLeadAB(ref mGatePoly[0], ref mGatePoly[1], 0, hs);
			InterpolationPost(ref mGatePoly[2], 0.5 + ww / Post.Len);
			InterpolationPost(ref mCtrlTerm, 0.5, -hs);
			InterpolationPost(ref mCtrlLead, 0.5, -hs / 2);
			mElm.SetNodePos(Post.A, Post.B, mCtrlTerm);
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			DrawPolygon(mGatePoly);
			DrawLine(mCtrlTerm, mCtrlLead);
			UpdateDotCount(mElm.Current, ref mCurCount);
			DrawCurrentB(mCurCount);
		}

		public override void Drag(Point pos) {
			pos = SnapGrid(pos);
			if (Math.Abs(Post.A.X - pos.X) < Math.Abs(Post.A.Y - pos.Y)) {
				pos.X = Post.A.X;
			} else {
				pos.Y = Post.A.Y;
			}
			int q1 = Math.Abs(Post.A.X - pos.X) + Math.Abs(Post.A.Y - pos.Y);
			int q2 = (q1 / 2) % GRID_SIZE;
			if (q2 != 0) {
				return;
			}
			Post.B = pos;
			SetPoints();
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "tri-state buffer";
			arr[1] = mElm.Open ? "open" : "closed";
			arr[2] = "Vd = " + TextUtils.VoltageAbs(mElm.GetVoltageDiff());
			arr[3] = "I = " + TextUtils.CurrentAbs(mElm.Current);
			arr[4] = "Vc = " + TextUtils.Voltage(mElm.NodeVolts[2]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("オン抵抗(Ω)", mElm.Ron);
			}
			if (r == 1) {
				return new ElementInfo("オフ抵抗(Ω)", mElm.Roff);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0 && ei.Value > 0) {
				mElm.Ron = ei.Value;
			}
			if (n == 1 && ei.Value > 0) {
				mElm.Roff = ei.Value;
			}
		}
	}
}
