using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
	class AM : BaseSymbol {
		const int FLAG_COS = 2;
		const int SIZE = 28;

		ElmAM mElm;

		public override BaseElement Element { get { return mElm; } }

		public AM(Point pos) : base(pos) {
			mElm = new ElmAM();
		}

		public AM(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmAM(st);
			if ((mFlags & FLAG_COS) != 0) {
				mFlags &= ~FLAG_COS;
			}
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.AM; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.CarrierFreq);
			optionList.Add(mElm.SignalFreq);
			optionList.Add(mElm.MaxVoltage);
			optionList.Add(mElm.Phase);
			optionList.Add(mElm.Depth);
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLead1(1 - 0.5 * SIZE / Post.Len);
			InterpolationPost(ref mNamePos, 1);
			ReferenceName = "AM";
		}

		public override void Draw(CustomGraphics g) {
			DrawLeadA();
			DrawCircle(Post.B, SIZE / 2);
			DrawCenteredText(ReferenceName, mNamePos);
			UpdateDotCount(-mElm.Current, ref mCurCount);
			if (ConstructItem != this) {
				DrawCurrentA(mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "AM Source";
			arr[1] = "I = " + Utils.CurrentText(mElm.Current);
			arr[2] = "V = " + Utils.VoltageText(mElm.VoltageDiff);
			arr[3] = "cf = " + Utils.FrequencyText(mElm.CarrierFreq);
			arr[4] = "sf = " + Utils.FrequencyText(mElm.SignalFreq);
			arr[5] = "Vmax = " + Utils.VoltageText(mElm.MaxVoltage);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("振幅", mElm.MaxVoltage);
			}
			if (r == 1) {
				return new ElementInfo("搬送波周波数", mElm.CarrierFreq);
			}
			if (r == 2) {
				return new ElementInfo("信号周波数", mElm.SignalFreq);
			}
			if (r == 3) {
				return new ElementInfo("変調度(%)", (int)(mElm.Depth * 100));
			}
			if (r == 4) {
				return new ElementInfo("位相(deg)", double.Parse((mElm.Phase * 180 / Math.PI).ToString("0.00")));
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.MaxVoltage = ei.Value;
			}
			if (n == 1) {
				mElm.CarrierFreq = ei.Value;
			}
			if (n == 2) {
				mElm.SignalFreq = ei.Value;
			}
			if (n == 3) {
				mElm.Depth = ei.Value * 0.01;
			}
			if (n == 4) {
				mElm.Phase = ei.Value * Math.PI / 180;
			}
		}
	}
}
