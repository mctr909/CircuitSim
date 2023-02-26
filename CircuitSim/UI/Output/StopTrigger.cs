using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class StopTrigger : BaseUI {
		public StopTrigger(Point pos) : base(pos) {
			Elm = new ElmStopTrigger();
		}

		public StopTrigger(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
			Elm = new ElmStopTrigger(st);
		}

		public override DUMP_ID DumpType { get { return DUMP_ID.STOP_TRIGGER; } }

		protected override void dump(List<object> optionList) {
			var ce = (ElmStopTrigger)Elm;
			optionList.Add(ce.TriggerVoltage);
			optionList.Add(ce.Type);
			optionList.Add(ce.Delay);
		}

	 	public override void SetPoints() {
			base.SetPoints();
			mLead1 = new Point();
		}

		public override void Draw(CustomGraphics g) {
			string s = "trigger";
			double w = g.GetTextSize(s).Width / 2;
			if (w > mLen * 0.8) {
				w = mLen * 0.8;
			}
			setLead1(1 - w / mLen);
			setBbox(Elm.Post1X, Elm.Post1Y, mLead1, 0);
			drawCenteredText(s, DumpInfo.P2X, DumpInfo.P2Y, true);
			drawLeadA();
			drawPosts();
		}

		public override void GetInfo(string[] arr) {
			var ce = (ElmStopTrigger)Elm;
			arr[0] = "stop trigger";
			arr[1] = "V = " + Utils.VoltageText(ce.Volts[0]);
			arr[2] = "Vtrigger = " + Utils.VoltageText(ce.TriggerVoltage);
			arr[3] = ce.Triggered ? ("stopping in "
				+ Utils.UnitText(ce.TriggerTime + ce.Delay - CirSimForm.Time, "s")) : ce.Stopped ? "stopped" : "waiting";
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			var ce = (ElmStopTrigger)Elm;
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				var ei = new ElementInfo("閾値電圧(V)", ce.TriggerVoltage);
				return ei;
			}
			if (r == 1) {
				return new ElementInfo("トリガータイプ", ce.Type, new string[] { ">=", "<=" });
			}
			if (r == 2) {
				var ei = new ElementInfo("遅延(s)", ce.Delay);
				return ei;
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			var ce = (ElmStopTrigger)Elm;
			if (n == 0) {
				ce.TriggerVoltage = ei.Value;
			}
			if (n == 1) {
				ce.Type = ei.Choice.SelectedIndex;
			}
			if (n == 2) {
				ce.Delay = ei.Value;
			}
		}
	}
}
