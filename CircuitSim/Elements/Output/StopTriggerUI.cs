using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class StopTriggerUI : BaseUI {
		public StopTriggerUI(Point pos) : base(pos) {
			Elm = new StopTriggerElm();
		}

		public StopTriggerUI(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
			Elm = new StopTriggerElm(st);
		}

		public override DUMP_ID DumpType { get { return DUMP_ID.STOP_TRIGGER; } }

		protected override void dump(List<object> optionList) {
			var ce = (StopTriggerElm)Elm;
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
			setBbox(mPost1, mLead1, 0);
			drawCenteredText(s, DumpInfo.P2, true);
			drawLead(mPost1, mLead1);
			drawPosts();
		}

		public override void GetInfo(string[] arr) {
			var ce = (StopTriggerElm)Elm;
			arr[0] = "stop trigger";
			arr[1] = "V = " + Utils.VoltageText(ce.Volts[0]);
			arr[2] = "Vtrigger = " + Utils.VoltageText(ce.TriggerVoltage);
			arr[3] = ce.Triggered ? ("stopping in "
				+ Utils.UnitText(ce.TriggerTime + ce.Delay - CirSimForm.Sim.Time, "s")) : ce.Stopped ? "stopped" : "waiting";
		}

		public override ElementInfo GetElementInfo(int n) {
			var ce = (StopTriggerElm)Elm;
			if (n == 0) {
				var ei = new ElementInfo("閾値電圧(V)", ce.TriggerVoltage);
				return ei;
			}
			if (n == 1) {
				var ei = new ElementInfo("トリガータイプ", ce.Type, -1, -1);
				ei.Choice = new ComboBox();
				ei.Choice.Items.Add(">=");
				ei.Choice.Items.Add("<=");
				ei.Choice.SelectedIndex = ce.Type;
				return ei;
			}
			if (n == 2) {
				var ei = new ElementInfo("遅延(s)", ce.Delay);
				return ei;
			}
			return null;
		}

		public override void SetElementValue(int n, ElementInfo ei) {
			var ce = (StopTriggerElm)Elm;
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
