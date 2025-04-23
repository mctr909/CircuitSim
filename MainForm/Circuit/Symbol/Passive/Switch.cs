using Circuit.Elements.Passive;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Passive {
	class Switch : BaseSymbol {
		const int OPEN_HS = 12;
		const int BODY_LEN = 24;

		protected ElmSwitch mElm;
		public bool Momentary = false;
		public int PosCount = 2;
		public int Group = 0;

		public override bool IsWire { get { return mElm.Position == 0; } }

		public override int VoltageSourceCount { get { return (1 == mElm.Position) ? 0 : 1; } }
		public override bool HasConnection(int n1, int n2) { return 0 == mElm.Position; }

		public Switch(Point pos, bool momentary = false, bool isOff = false) : base(pos) {
			mElm = (ElmSwitch)Element;
			Momentary = momentary;
			mElm.Position = isOff ? 1 : 0;
		}

		public Switch(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = (ElmSwitch)Element;
			mElm.Position = st.nextTokenInt();
			Momentary = st.nextTokenBool(false);
			Group = st.nextTokenInt();
		}

		protected Switch(Point pos, int dummy) : base(pos) {
			mElm = (ElmSwitch)Element;
		}

		protected Switch(Point p1, Point p2, int f) : base(p1, p2, f) {
			mElm = (ElmSwitch)Element;
		}

		protected override BaseElement Create() {
			return new ElmSwitch();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.SWITCH; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Position);
			optionList.Add(Momentary);
			optionList.Add(Group);
		}

		public override void Stamp() {
			if (mElm.Position == 0) {
				StampVoltageSource(mElm.Nodes[0], mElm.Nodes[1], mElm.VoltSource, 0);
			}
		}

		public void MouseUp() {
			if (Momentary) {
				Toggle();
			}
		}

		public void Toggle() {
			mElm.Position++;
			if (PosCount <= mElm.Position) {
				mElm.Position = 0;
			}
			if (Group != 0) {
				int i;
				for (i = 0; i != MainForm.MainForm.SymbolCount; i++) {
					var symbol2 = MainForm.MainForm.SymbolList[i];
					if (symbol2 == this) {
						continue;
					}
					if (this is SwitchMulti) {
						if (symbol2 is SwitchMulti sw2) {
							var s2 = sw2.mElm;
							if (sw2.Group == Group) {
								if (mElm.Position < s2.ThrowCount) {
									s2.Position = mElm.Position;
								}
							}
						}
					} else {
						if (symbol2 is Switch sw2) {
							if (sw2.Group == Group) {
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
			arr[0] = Momentary ? "プッシュスイッチ(" : "スイッチ(";
			if (mElm.Position == 1) {
				arr[0] += "OFF)";
				arr[1] = "電位差：" + TextUtils.VoltageAbs(mElm.VoltageDiff);
			} else {
				arr[0] += "ON)";
				arr[1] = "電位：" + TextUtils.Voltage(mElm.V[0]);
				arr[2] = "電流：" + TextUtils.CurrentAbs(mElm.I[0]);
			}
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("連動グループ", Group);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				Group = (int)ei.Value;
			}
		}
	}
}
