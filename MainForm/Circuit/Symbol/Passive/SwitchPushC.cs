using MainForm.Forms;

namespace Circuit.Symbol.Passive {
	class SwitchPushC : SwitchMulti {
		public SwitchPushC(Point pos) : base(pos) {
			mElm.Position = 1;
			Momentary = true;
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
