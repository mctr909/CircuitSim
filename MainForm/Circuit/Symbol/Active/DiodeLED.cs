using Circuit.Forms;

namespace Circuit.Symbol.Active {
	class DiodeLED : Diode {
		const int CR = 10;
		const int CR_INNER = 7;

		static string mLastLEDModelName = "default-led";

		double mMaxBrightnessCurrent;
		double mColorR;
		double mColorG;
		double mColorB;

		PointF mLedLead1;
		PointF mLedLead2;
		PointF mLedCenter;

		public DiodeLED(Point pos) : base(pos, "D") {
			ModelName = mLastLEDModelName;
			var model = DiodeModel.GetModelWithName(ModelName);
			mElm.VZener = model.BreakdownVoltage;
			mElm.FwDrop = model.FwDrop;
			mElm.Leakage = model.SaturationCurrent;
			mElm.VScale = model.VScale;
			mElm.VdCoef = model.VdCoef;
			mElm.SeriesResistance = model.SeriesResistance;
			mElm.Model = model;
			mElm.Setup();
			mMaxBrightnessCurrent = 0.01;
			mColorR = 1;
			mColorG = mColorB = 0;
		}

		public DiodeLED(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
			if ((f & (FLAG_MODEL | FLAG_FWDROP)) == 0) {
				const double fwdrop = 2.1024259;
				ModelName = DiodeModel.GetModelWithParameters(fwdrop, 0).Name;
				var model = DiodeModel.GetModelWithName(ModelName);
				mElm.VZener = model.BreakdownVoltage;
				mElm.FwDrop = model.FwDrop;
				mElm.Leakage = model.SaturationCurrent;
				mElm.VScale = model.VScale;
				mElm.VdCoef = model.VdCoef;
				mElm.SeriesResistance = model.SeriesResistance;
				mElm.Model = model;
				mElm.Setup();
			}
			mColorR = st.nextTokenDouble(1.0);
			mColorG = st.nextTokenDouble();
			mColorB = st.nextTokenDouble();
			mMaxBrightnessCurrent = st.nextTokenDouble(1e-3);
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.LED; } }

		protected override void dump(List<object> optionList) {
			base.dump(optionList);
			optionList.Add(mColorR);
			optionList.Add(mColorG);
			optionList.Add(mColorB);
			optionList.Add(mMaxBrightnessCurrent);
		}

		public override void SetPoints() {
			base.SetPoints();
			InterpolationPost(ref mLedLead1, 0.5 - CR / Post.Len);
			InterpolationPost(ref mLedLead2, 0.5 + CR / Post.Len);
			InterpolationPost(ref mLedCenter, 0.5);
		}

		public override void Draw(CustomGraphics g) {
			if (g is PDF.Page || NeedsHighlight || this == ConstructItem) {
				base.Draw(g);
			} else {
				var lum = mElm.Current / mMaxBrightnessCurrent;
				if (0 < lum) {
					lum = 255 * (1 + .2 * Math.Log(lum));
				}
				if (255 < lum) {
					lum = 255;
				}
				if (lum < 0) {
					lum = 0;
				}
				DrawLine(Post.A, mLedLead1);
				DrawLine(mLedLead2, Post.B);
				DrawCircle(mLedCenter, CR);
				var bk = g.FillColor;
				g.FillColor = Color.FromArgb((int)(mColorR * lum), (int)(mColorG * lum), (int)(mColorB * lum));
				FillCircle(mLedCenter, CR_INNER);
				g.FillColor = bk;
				UpdateDotCount();
				DrawCurrent(Post.A, mLedLead1, mCurCount);
				DrawCurrent(Post.B, mLedLead2, -mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			base.GetInfo(arr);
			arr[0] = "LED";
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("赤(0～1)", mColorR);
			}
			if (r == 1) {
				return new ElementInfo("緑(0～1)", mColorG);
			}
			if (r == 2) {
				return new ElementInfo("青(0～1)", mColorB);
			}
			if (r == 3) {
				return new ElementInfo("最大輝度電流(A)", mMaxBrightnessCurrent);
			}
			return base.GetElementInfo(r - 4, c);
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mColorR = ei.Value;
			}
			if (n == 1) {
				mColorG = ei.Value;
			}
			if (n == 2) {
				mColorB = ei.Value;
			}
			if (n == 3) {
				mMaxBrightnessCurrent = ei.Value;
			}
			base.SetElementValue(n - 4, c, ei);
		}
	}
}
