using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
	class FM : BaseSymbol {
		const int FLAG_COS = 2;
		const int SIZE = 28;

		ElmFM mElm;

		public FM(Point pos) : base(pos) {
			mElm = new ElmFM();
			Elm = mElm;
		}

		public FM(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmFM(st);
			Elm = mElm;
			if ((mFlags & FLAG_COS) != 0) {
				mFlags &= ~FLAG_COS;
			}
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.FM; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.CarrierFreq);
			optionList.Add(mElm.Signalfreq);
			optionList.Add(mElm.MaxVoltage);
			optionList.Add(mElm.Deviation);
		}

		public override void SetPoints() {
			base.SetPoints();
			setLead1(1 - 0.5 * SIZE / Post.Len);
			interpPost(ref mNamePos, 1);
			ReferenceName = "FM";
		}

		public override void Draw(CustomGraphics g) {
			drawLeadA();
			drawCircle(Post.B, SIZE / 2);
			drawCenteredText(ReferenceName, mNamePos);
			updateDotCount(-mElm.Current, ref mCurCount);
			if (CirSimForm.ConstructElm != this) {
				drawCurrentA(mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "FM Source";
			arr[1] = "I = " + Utils.CurrentText(mElm.Current);
			arr[2] = "V = " + Utils.VoltageText(mElm.VoltageDiff);
			arr[3] = "cf = " + Utils.FrequencyText(mElm.CarrierFreq);
			arr[4] = "sf = " + Utils.FrequencyText(mElm.Signalfreq);
			arr[5] = "dev =" + Utils.FrequencyText(mElm.Deviation);
			arr[6] = "Vmax = " + Utils.VoltageText(mElm.MaxVoltage);
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
				return new ElementInfo("信号周波数", mElm.Signalfreq);
			}
			if (r == 3) {
				return new ElementInfo("周波数偏移(Hz)", mElm.Deviation);
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
				mElm.Signalfreq = ei.Value;
			}
			if (n == 3) {
				mElm.Deviation = ei.Value;
			}
		}
	}
}
