﻿using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.Symbol.Output {
    class StopTrigger : BaseSymbol {
		public StopTrigger(Point pos) : base(pos) {
			Elm = new ElmStopTrigger();
		}

		public StopTrigger(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
			Elm = new ElmStopTrigger(st);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.STOP_TRIGGER; } }

		protected override void dump(List<object> optionList) {
			var ce = (ElmStopTrigger)Elm;
			optionList.Add(ce.TriggerVoltage.ToString("g3"));
			optionList.Add(ce.Type);
			optionList.Add(ce.Delay.ToString("g3"));
		}

	 	public override void SetPoints() {
			base.SetPoints();
			mLead1 = new Point();
			setTextPos();
		}

		void setTextPos() {
			ReferenceName = "stop trigger";
			var txtSize = CustomGraphics.Instance.GetTextSize(ReferenceName);
			var txtW = txtSize.Width;
			var txtH = txtSize.Height;
			var pw = txtW / Post.Len;
			setLead1(1);
			var abX = Post.B.X - Post.A.X;
			var abY = Post.B.Y - Post.A.Y;
			mTextRot = Math.Atan2(abY, abX);
			var deg = -mTextRot * 180 / Math.PI;
			if (deg < 0.0) {
				deg += 360;
			}
			if (45 * 3 <= deg && deg < 45 * 7) {
				mTextRot += Math.PI;
				interpPost(ref mNamePos, 1 + 0.5 * pw, txtH / Post.Len);
			} else {
				interpPost(ref mNamePos, 1 + 0.5 * pw, -txtH / Post.Len);
			}
		}

		public override void Draw(CustomGraphics g) {
			drawCenteredText(ReferenceName, mNamePos, mTextRot);
			drawLeadA();
		}

		public override void GetInfo(string[] arr) {
			var ce = (ElmStopTrigger)Elm;
			arr[0] = "stop trigger";
			arr[1] = "V = " + Utils.VoltageText(ce.Volts[0]);
			arr[2] = "Vtrigger = " + Utils.VoltageText(ce.TriggerVoltage);
			arr[3] = ce.Triggered ? ("stopping in "
				+ Utils.TimeText(ce.TriggerTime + ce.Delay - Circuit.Time)) : ce.Stopped ? "stopped" : "waiting";
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			var ce = (ElmStopTrigger)Elm;
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				var ei = new ElementInfo("閾値電圧", ce.TriggerVoltage);
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