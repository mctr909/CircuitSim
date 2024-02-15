using Circuit.Forms;
using Circuit.Elements.Output;

namespace Circuit.Symbol.Output {
	class LogicOutput : BaseSymbol {
		const int FLAG_TERNARY = 1;
		const int FLAG_NUMERIC = 2;
		const int FLAG_PULLDOWN = 4;

		ElmLogicOutput mElm;

		public override BaseElement Element { get { return mElm; } }

		public LogicOutput(Point pos) : base(pos) {
			mElm = new ElmLogicOutput();
		}

		public LogicOutput(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmLogicOutput();
			mElm.Threshold = st.nextTokenDouble(2.5);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.LOGIC_O; } }

		bool isTernary { get { return (mFlags & FLAG_TERNARY) != 0; } }

		bool isNumeric { get { return (mFlags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; } }

		bool needsPullDown { get { return (mFlags & FLAG_PULLDOWN) != 0; } }

		public override void SetPoints() {
			base.SetPoints();
			SetLead1(1 - 12 / Post.Len);
		}

		public override void Draw(CustomGraphics g) {
			var s = (mElm.Volts[0] < mElm.Threshold) ? "L" : "H";
			if (isTernary) {
				if (mElm.Volts[0] > 3.75) {
					s = "2";
				} else if (mElm.Volts[0] > 1.25) {
					s = "1";
				} else {
					s = "0";
				}
			} else if (isNumeric) {
				s = (mElm.Volts[0] < mElm.Threshold) ? "0" : "1";
			}
			mElm.Value = s;
			DrawCenteredLText(s, Post.B);
			DrawLeadA();
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "ロジック出力";
			arr[1] = (mElm.Volts[0] < mElm.Threshold) ? "Low" : "High";
			if (isNumeric) {
				arr[1] = mElm.Value;
			}
			arr[2] = "電位：" + TextUtils.Voltage(mElm.Volts[0]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("閾値(V)", mElm.Threshold);
			}
			if (r == 1) {
				return new ElementInfo("プルダウン", needsPullDown);
			}
			if (r == 2) {
				return new ElementInfo("数値表示", isNumeric);
			}
			if (r == 3) {
				return new ElementInfo("3値", isTernary);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.Threshold = ei.Value;
			}
			if (n == 1) {
				if (ei.CheckBox.Checked) {
					mFlags = FLAG_PULLDOWN;
				} else {
					mFlags &= ~FLAG_PULLDOWN;
				}
				mElm.NeedsPullDown = needsPullDown;
			}
			if (n == 2) {
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_NUMERIC;
				} else {
					mFlags &= ~FLAG_NUMERIC;
				}
			}
			if (n == 3) {
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_TERNARY;
				} else {
					mFlags &= ~FLAG_TERNARY;
				}
			}
		}
	}
}
