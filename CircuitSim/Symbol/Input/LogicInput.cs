using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;
using Circuit.Symbol.Passive;

namespace Circuit.Symbol.Input {
	class LogicInput : Switch {
		const int FLAG_NUMERIC = 2;

		ElmLogicInput mElm;

		bool isNumeric { get { return (mFlags & (FLAG_NUMERIC)) != 0; } }

		public LogicInput(Point pos) : base(pos, 0) {
			mElm = new ElmLogicInput();
		}

		public LogicInput(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmLogicInput(st);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.LOGIC_I; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.mHiV);
			optionList.Add(mElm.mLoV);
		}

		public override RectangleF GetSwitchRect() {
			return new RectangleF(Post.B.X - 10, Post.B.Y - 10, 20, 20);
		}

		public override void SetPoints() {
			base.SetPoints();
			setLead1(1 - 12 / Post.Len);
		}

		public override void Draw(CustomGraphics g) {
			var s = 0 != mElm.Position ? "H" : "L";
			if (isNumeric) {
				s = "" + mElm.Position;
			}
			drawCenteredLText(s, Post.B, true);
			drawLeadA();
			updateDotCount();
			drawCurrentA(mCurCount);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "ロジック入力";
			arr[1] = 0 != mElm.Position ? "High" : "Low";
			if (isNumeric) {
				arr[1] = 0 != mElm.Position ? "1" : "0";
			}
			arr[1] += " (" + Utils.VoltageText(mElm.Volts[0]) + ")";
			arr[2] = "電流：" + Utils.CurrentText(mElm.Current);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("モーメンタリ", mElm.Momentary);
			}
			if (r == 1) {
				return new ElementInfo("High電圧", mElm.mHiV);
			}
			if (r == 2) {
				return new ElementInfo("Low電圧", mElm.mLoV);
			}
			if (r == 3) {
				return new ElementInfo("数値表示", isNumeric);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.Momentary = ei.CheckBox.Checked;
			}
			if (n == 1) {
				mElm.mHiV = ei.Value;
			}
			if (n == 2) {
				mElm.mLoV = ei.Value;
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
