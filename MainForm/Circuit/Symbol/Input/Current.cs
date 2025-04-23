using Circuit.Elements.Input;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Input {
	class Current : BaseSymbol {
		const int BODY_LEN = 24;

		PointF[] mArrow;
		PointF mAshaft1;
		PointF mAshaft2;
		PointF mCenter;
		PointF mTextPos;
		ElmCurrent mElm;

		public Current(Point pos) : base(pos) {
			mElm = (ElmCurrent)Element;
		}

		public Current(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = (ElmCurrent)Element;
			mElm.CurrentValue = st.nextTokenDouble();
		}

		protected override BaseElement Create() {
			return new ElmCurrent();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.CURRENT; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.CurrentValue);
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLeads(BODY_LEN);
			InterpolationLead(ref mAshaft1, 0.25);
			InterpolationLead(ref mAshaft2, 0.6);
			InterpolationLead(ref mCenter, 0.5);
			int sign;
			if (Post.Horizontal) {
				sign = Post.Dsign;
			} else {
				sign = -Post.Dsign;
			}
			InterpolationPost(ref mTextPos, 0.5, 30 * sign);
			var p2 = new PointF();
			InterpolationLead(ref p2, 0.8);
			CreateArrow(mCenter, p2, out mArrow, 8, 4);
		}

		public void StampCurrentSource(bool broken) {
			if (broken) {
				/* no current path; stamping a current source would cause a matrix error. */
				StampResistor(mElm.Nodes[0], mElm.Nodes[1], 1e8);
				mElm.I[0] = 0;
			} else {
				/* ok to stamp a current source */
				StampCurrent(mElm.Nodes[0], mElm.Nodes[1], mElm.CurrentValue);
				mElm.I[0] = mElm.CurrentValue;
			}
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			DrawCircle(mCenter, BODY_LEN / 2);
			DrawLine(mAshaft1, mAshaft2);
			FillPolygon(mArrow);
			DoDots();
			if (ControlPanel.ChkShowValues.Checked) {
				var s = TextUtils.Current(mElm.CurrentValue);
				DrawCenteredText(s, mTextPos);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "電流源";
			GetBasicInfo(1, arr);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("電流(A)", mElm.CurrentValue);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			mElm.CurrentValue = ei.Value;
		}
	}
}
