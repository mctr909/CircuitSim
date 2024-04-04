using Circuit.Forms;
using Circuit.Elements.Input;
using Circuit.Symbol.Passive;

namespace Circuit.Symbol.Input {
	class LogicInput : Switch {
		const int FLAG_NUMERIC = 2;

		ElmLogicInput mElmLogic;

		bool isNumeric { get { return (mFlags & (FLAG_NUMERIC)) != 0; } }

		public LogicInput(Point pos) : base(pos, 0) {
			mElmLogic = new ElmLogicInput();
			mElm = mElmLogic;
		}

		public LogicInput(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElmLogic = new ElmLogicInput();
			mElmLogic.VHigh = st.nextTokenDouble(5);
			mElmLogic.VLow = st.nextTokenDouble(0);
			mElm = mElmLogic;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.LOGIC_I; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElmLogic.VHigh);
			optionList.Add(mElmLogic.VLow);
		}

		public override RectangleF GetSwitchRect() {
			return new RectangleF(Post.B.X - 10, Post.B.Y - 10, 20, 20);
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLead1(1 - 12 / Post.Len);
		}

		public override void Draw(CustomGraphics g) {
			var s = 0 != mElmLogic.Position ? "H" : "L";
			if (isNumeric) {
				s = "" + mElmLogic.Position;
			}
			DrawCenteredLText(s, Post.B);
			DrawLeadA();
			UpdateDotCount();
			DrawCurrentA(mCurCount);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "ロジック入力";
			arr[1] = 0 != mElmLogic.Position ? "High" : "Low";
			if (isNumeric) {
				arr[1] = 0 != mElmLogic.Position ? "1" : "0";
			}
			arr[1] += " (" + TextUtils.Voltage(mElmLogic.Volts[0]) + ")";
			arr[2] = "電流：" + TextUtils.Current(mElmLogic.Current);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("モーメンタリ", mElmLogic.Momentary);
			}
			if (r == 1) {
				return new ElementInfo("High電圧", mElmLogic.VHigh);
			}
			if (r == 2) {
				return new ElementInfo("Low電圧", mElmLogic.VLow);
			}
			if (r == 3) {
				return new ElementInfo("数値表示", isNumeric);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElmLogic.Momentary = ei.CheckBox.Checked;
			}
			if (n == 1) {
				mElmLogic.VHigh = ei.Value;
			}
			if (n == 2) {
				mElmLogic.VLow = ei.Value;
			}
			if (n == 3) {
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_NUMERIC;
				} else {
					mFlags &= ~FLAG_NUMERIC;
				}
			}
		}
	}
}
