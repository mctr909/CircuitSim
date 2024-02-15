using Circuit.Forms;
using Circuit.Elements.Gate;

namespace Circuit.Symbol.Gate {
	abstract class Gate : BaseSymbol {
		const int FLAG_SMALL = 1;
		const int FLAG_SCHMITT = 2;

		const int G_WIDTH = 6;
		const int G_WIDTH2 = 12;
		const int G_HEIGHT = 8;
		const int CIRCLE_SIZE = 3;

		static bool mLastSchmitt = false;

		protected int mHs2;
		protected int mWw;

		protected ElmGate mElm;

		protected PointF[] mGatePolyEuro;
		protected PointF[] mGatePolyAnsi;

		protected PointF mCirclePos;
		protected PointF[] mLinePoints;

		PointF[] mSchmittPoly;
		PointF[] mInPosts;
		PointF[] mInGates;

		protected virtual string gateText { get { return null; } }

		protected virtual string gateName { get { return ""; } }

		protected Gate(Point pos) : base(pos) {
			Post.NoDiagonal = true;
			if (mLastSchmitt) {
				mFlags |= FLAG_SCHMITT;
			}
			mFlags |= FLAG_SMALL;
		}

		protected Gate(Point p1, Point p2, int f) : base(p1, p2, f) {
			Post.NoDiagonal = true;
			mFlags |= FLAG_SMALL;
		}

		public static bool UseAnsiGates() { return ControlPanel.ChkUseAnsiSymbols.Checked; }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.InputCount);
			optionList.Add(mElm.Volts[mElm.InputCount]);
			optionList.Add(mElm.HighVoltage);
		}

		public override void SetPoints() {
			base.SetPoints();
			mElm.InputStates = new bool[mElm.InputCount];
			int hs = G_HEIGHT;
			int i;
			mWw = G_WIDTH2;
			if (mWw > Post.Len / 2) {
				mWw = (int)(Post.Len / 2);
			}
			if (mElm.IsInverting && mWw + 8 > Post.Len / 2) {
				mWw = (int)(Post.Len / 2 - 8);
			}
			SetLeads(mWw * 2);
			mInPosts = new PointF[mElm.InputCount];
			mInGates = new PointF[mElm.InputCount];
			mElm.AllocNodes();
			int i0 = -mElm.InputCount / 2;
			for (i = 0; i != mElm.InputCount; i++, i0++) {
				if (i0 == 0 && (mElm.InputCount & 1) == 0) {
					i0++;
				}
				InterpolationPost(ref mInPosts[i], 0, hs * i0);
				InterpolationLead(ref mInGates[i], 0, hs * i0);
				mElm.Volts[i] = (mElm.LastOutput ^ mElm.IsInverting) ? 5 : 0;
			}
			mHs2 = G_WIDTH * (mElm.InputCount / 2 + 1);
			if (mElm.HasSchmittInputs) {
				CreateSchmitt(mLead1, mLead2, out mSchmittPoly, 1, .47f);
			}
			mElm.SetNodePos(mInPosts, Post.B);
		}

		public override void Draw(CustomGraphics g) {
			for (int i = 0; i != mElm.InputCount; i++) {
				DrawLine(mInPosts[i], mInGates[i]);
			}
			DrawLeadB();
			if (UseAnsiGates()) {
				DrawPolygon(mGatePolyAnsi);
			} else {
				DrawPolygon(mGatePolyEuro);
				var center = new PointF();
				InterpolationPost(ref center, 0.5);
				DrawCenteredLText(gateText, center);
			}
			if (mElm.HasSchmittInputs) {
				DrawPolygon(mSchmittPoly);
			}
			if (mLinePoints != null && UseAnsiGates()) {
				for (int i = 0; i != mLinePoints.Length - 1; i++) {
					DrawLine(mLinePoints[i], mLinePoints[i + 1]);
				}
			}
			if (mElm.IsInverting) {
				DrawCircle(mCirclePos, CIRCLE_SIZE);
			}
			UpdateDotCount(mElm.Current, ref mCurCount);
			DrawCurrentB(mCurCount);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = gateName;
			arr[1] = "Vout：" + TextUtils.Voltage(mElm.Volts[mElm.InputCount]);
			arr[2] = "Iout：" + TextUtils.Current(mElm.Current);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("入力数", mElm.InputCount);
			}
			if (r == 1) {
				return new ElementInfo("High電圧", mElm.HighVoltage);
			}
			if (r == 2) {
				return new ElementInfo("シュミットトリガー", mElm.HasSchmittInputs);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0 && ei.Value >= 1) {
				mElm.InputCount = (int)ei.Value;
				SetPoints();
			}
			if (n == 1) {
				mElm.HighVoltage = ElmGate.LastHighVoltage = ei.Value;
			}
			if (n == 2) {
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_SCHMITT;
				} else {
					mFlags &= ~FLAG_SCHMITT;
				}
				mLastSchmitt = mElm.HasSchmittInputs = 0 != (mFlags & FLAG_SCHMITT);
				SetPoints();
			}
		}

		protected void CreateEuroGatePolygon() {
			mGatePolyEuro = new PointF[4];
			InterpolationLeadAB(ref mGatePolyEuro[0], ref mGatePolyEuro[1], 0, mHs2);
			InterpolationLeadAB(ref mGatePolyEuro[3], ref mGatePolyEuro[2], 1, mHs2);
		}
	}
}
