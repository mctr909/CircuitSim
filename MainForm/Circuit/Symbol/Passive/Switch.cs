using Circuit.Forms;
using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
	class Switch : BaseSymbol {
		const int OPEN_HS = 12;
		const int BODY_LEN = 24;

		protected ElmSwitch mElm;

		public override BaseElement Element { get { return mElm; } }

		public Switch(Point pos, int dummy) : base(pos) { }

		public Switch(Point pos, bool momentary = false, bool isNo = false) : base(pos) {
			mElm = new ElmSwitch {
				Momentary = momentary,
				Position = isNo ? 1 : 0
			};
		}

		public Switch(Point p1, Point p2, int f) : base(p1, p2, f) { }

		public Switch(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmSwitch {
				Position = st.nextTokenInt(),
				Momentary = st.nextTokenBool(false),
				Link = st.nextTokenInt()
			};
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.SWITCH; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Position);
			optionList.Add(mElm.Momentary);
			optionList.Add(mElm.Link);
		}

		public void MouseUp() {
			if (mElm.Momentary) {
				Toggle();
			}
		}

		public void Toggle() {
			mElm.Position++;
			if (mElm.PosCount <= mElm.Position) {
				mElm.Position = 0;
			}
			if (mElm.Link != 0) {
				int i;
				for (i = 0; i != CircuitSymbol.Count; i++) {
					var symbol2 = CircuitSymbol.List[i];
					if (symbol2 == this) {
						continue;
					}
					if (this is SwitchMulti) {
						if (symbol2 is SwitchMulti sw2) {
							var s2 = (ElmSwitchMulti)sw2.mElm;
							if (s2.Link == mElm.Link) {
								if (mElm.Position < s2.ThrowCount) {
									s2.Position = mElm.Position;
								}
							}
						}
					} else {
						if (symbol2 is Switch sw2) {
							if (sw2.mElm.Link == mElm.Link) {
								sw2.mElm.Position = sw2.mElm.Position == 0 ? 1 : 0;
							}
						}
					}
				}
			}
		}

		public virtual RectangleF GetSwitchRect() {
			var p1 = new PointF();
			InterpolationLead(ref p1, 0, 24);
			var l1 = new RectangleF(mLead1.X, mLead1.Y, 0, 0);
			var l2 = new RectangleF(mLead2.X, mLead2.Y, 0, 0);
			var p = new RectangleF(p1.X, p1.Y, 0, 0);
			return RectangleF.Union(l1, RectangleF.Union(l2, p));
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLeads(BODY_LEN);
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();
			FillCircle(mLead1, 2.5f);
			FillCircle(mLead2, 2.5f);
			/* draw switch */
			var p2 = new PointF();
			if (mElm.Position == 0) {
				InterpolationLead(ref p2, 1, 2);
				DoDots();
			} else {
				InterpolationLead(ref p2, (OPEN_HS - 2.0) / OPEN_HS, OPEN_HS);
			}
			DrawLine(mLead1, p2);
		}

		public override void GetInfo(string[] arr) {
			arr[0] = mElm.Momentary ? "プッシュスイッチ(" : "スイッチ(";
			if (mElm.Position == 1) {
				arr[0] += "OFF)";
				arr[1] = "電位差：" + TextUtils.VoltageAbs(mElm.VoltageDiff);
			} else {
				arr[0] += "ON)";
				arr[1] = "電位：" + TextUtils.Voltage(mElm.Volts[0]);
				arr[2] = "電流：" + TextUtils.CurrentAbs(mElm.Current);
			}
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("連動グループ", mElm.Link);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.Link = (int)ei.Value;
			}
		}
	}
}
