using Circuit.Forms;
using Circuit.Elements.Output;

namespace Circuit.Symbol.Output {
	class StopTrigger : BaseSymbol {
		ElmStopTrigger mElm;

		public override BaseElement Element { get { return mElm; } }

		public StopTrigger(Point pos) : base(pos) {
			mElm = new ElmStopTrigger();
		}

		public StopTrigger(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
			mElm = new ElmStopTrigger(st);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.STOP_TRIGGER; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.TriggerVoltage.ToString("g3"));
			optionList.Add(mElm.Type);
			optionList.Add(mElm.Delay.ToString("g3"));
		}

		public override void SetPoints() {
			base.SetPoints();
			mLead1 = new Point();
			SetTextPos();
		}

		void SetTextPos() {
			ReferenceName = "stop trigger";
			var txtSize = CustomGraphics.Instance.GetTextSize(ReferenceName);
			var txtW = txtSize.Width;
			var txtH = txtSize.Height;
			var pw = txtW / Post.Len;
			SetLead1(1);
			var abX = Post.B.X - Post.A.X;
			var abY = Post.B.Y - Post.A.Y;
			mTextRot = Math.Atan2(abY, abX);
			var deg = -mTextRot * 180 / Math.PI;
			if (deg < 0.0) {
				deg += 360;
			}
			if (45 * 3 <= deg && deg < 45 * 7) {
				mTextRot += Math.PI;
				InterpolationPost(ref mNamePos, 1 + 0.5 * pw, txtH / Post.Len);
			} else {
				InterpolationPost(ref mNamePos, 1 + 0.5 * pw, -txtH / Post.Len);
			}
		}

		public override void Draw(CustomGraphics g) {
			DrawCenteredText(ReferenceName, mNamePos, mTextRot);
			DrawLeadA();
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "stop trigger";
			arr[1] = "V = " + TextUtils.Voltage(mElm.Volts[0]);
			arr[2] = "Vtrigger = " + TextUtils.Voltage(mElm.TriggerVoltage);
			arr[3] = mElm.Triggered ? ("stopping in "
				+ TextUtils.Time(mElm.TriggerTime + mElm.Delay - CircuitElement.Time)) : mElm.Stopped ? "stopped" : "waiting";
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				var ei = new ElementInfo("閾値電圧", mElm.TriggerVoltage);
				return ei;
			}
			if (r == 1) {
				return new ElementInfo("トリガータイプ", mElm.Type, new string[] { ">=", "<=" });
			}
			if (r == 2) {
				var ei = new ElementInfo("遅延(s)", mElm.Delay);
				return ei;
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.TriggerVoltage = ei.Value;
			}
			if (n == 1) {
				mElm.Type = ei.Choice.SelectedIndex;
			}
			if (n == 2) {
				mElm.Delay = ei.Value;
			}
		}
	}
}
