﻿using Circuit.Elements.Logic;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Logic {
	class TriState : BaseSymbol {
		const int BODY_LEN = 16;

		ElmTriState mElm;
		PointF mCtrlLead;
		PointF mCtrlTerm;
		PointF[] mGatePoly;

		public override int InternalNodeCount { get { return 1; } }
		public override int VoltageSourceCount { get { return 1; } }
		/* there is no current path through the input, but there
         * is an indirect path through the output to ground. */
		public override bool HasConnection(int n1, int n2) { return false; }
		public override bool HasGroundConnection(int nodeIndex) { return nodeIndex == 1; }

		public TriState(Point pos) : base(pos) {
			mElm = (ElmTriState)Element;
		}

		public TriState(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = (ElmTriState)Element;
			mElm.Ron = st.nextTokenDouble(0.1);
			mElm.Roff = st.nextTokenDouble(1e10);
		}

		protected override BaseElement Create() {
			return new ElmTriState();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.TRISTATE; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Ron.ToString("g3"));
			optionList.Add(mElm.Roff.ToString("g3"));
		}

		public override void Stamp() {
			StampVoltageSource(0, mElm.Nodes[3], mElm.VoltSource);
			StampNonLinear(mElm.Nodes[3]);
			StampNonLinear(mElm.Nodes[1]);
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
			SetNodePos(Post.A, Post.B, mCtrlTerm);
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			DrawPolygon(mGatePoly);
			DrawLine(mCtrlTerm, mCtrlLead);
			UpdateDotCount(mElm.I[0], ref mCurCount);
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
			arr[2] = "Vd = " + TextUtils.VoltageAbs(mElm.VoltageDiff);
			arr[3] = "I = " + TextUtils.CurrentAbs(mElm.I[0]);
			arr[4] = "Vc = " + TextUtils.Voltage(mElm.V[2]);
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
