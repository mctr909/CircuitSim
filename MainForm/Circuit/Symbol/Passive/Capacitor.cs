using Circuit.Elements.Passive;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Passive {
	class Capacitor : BaseSymbol {
		public static readonly int FLAG_BACK_EULER = 2;
		protected static string mLastReferenceName = "C";
		protected static double mLastValue = 1e-5;
		public double Capacitance = 1e-5;

		const int BODY_LEN = 5;
		const int HS = 6;

		PointF[] mPlate1;
		PointF[] mPlate2;

		public Capacitor(Point pos) : base(pos) {
			Element.Para[ElmCapacitor.MAX_VOLTAGE] = 1e12;
			Element.Para[ElmCapacitor.MAX_NEGATIVE] = 1e12;
			Capacitance = mLastValue;
			ReferenceName = mLastReferenceName;
		}

		public Capacitor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			Element.Para[ElmCapacitor.MAX_VOLTAGE] = 1e12;
			Element.Para[ElmCapacitor.MAX_NEGATIVE] = 1e12;
			Capacitance = st.nextTokenDouble(mLastValue);
			Element.V[2] = st.nextTokenDouble(0);
		}

		protected Capacitor(Point p1, Point p2, int f) : base(p1, p2, f) {
			ReferenceName = mLastReferenceName;
		}

		protected override BaseElement Create() {
			return new ElmCapacitor();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.CAPACITOR; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(Capacitance.ToString("g3"));
			optionList.Add(Element.V[2].ToString("g3"));
		}

		public override void Reset() {
			base.Reset();
			Element.I[0] = Element.I[1] = 0;
			Element.V[2] = 1e-3;
		}

		public override void Stamp() {
			var n0 = Element.Nodes[0] - 1;
			var n1 = Element.Nodes[1] - 1;
			if (n0 < 0 || n1 < 0) {
				return;
			}
			Element.Para[0] = 0.5 * CircuitState.DeltaTime / Capacitance;
			StampResistor(Element.Nodes[0], Element.Nodes[1], Element.Para[0]);
			StampRightSide(Element.Nodes[0]);
			StampRightSide(Element.Nodes[1]);
		}

		public override void Shorted() {
			base.Reset();
			Element.V[2] = Element.I[0] = Element.I[1] = 0;
		}

		public override void SetPoints() {
			base.SetPoints();
			/* calc leads */
			SetLeads(BODY_LEN);
			/* calc plates */
			var dw = 0.8 / Post.Len;
			var f1 = 0.5 - BODY_LEN * 0.5 / Post.Len;
			var f2 = 0.5 + BODY_LEN * 0.5 / Post.Len;
			mPlate1 = new PointF[4];
			InterpolationPost(ref mPlate1[0], f1 - dw, -HS);
			InterpolationPost(ref mPlate1[1], f1 - dw, HS);
			InterpolationPost(ref mPlate1[2], f1 + dw, HS);
			InterpolationPost(ref mPlate1[3], f1 + dw, -HS);
			mPlate2 = new PointF[4];
			InterpolationPost(ref mPlate2[0], f2 - dw, -HS);
			InterpolationPost(ref mPlate2[1], f2 - dw, HS);
			InterpolationPost(ref mPlate2[2], f2 + dw, HS);
			InterpolationPost(ref mPlate2[3], f2 + dw, -HS);
			SetTextPos();
		}

		void SetTextPos() {
			var abX = Post.B.X - Post.A.X;
			var abY = Post.B.Y - Post.A.Y;
			mTextRot = Math.Atan2(abY, abX);
			var deg = -mTextRot * 180 / Math.PI;
			if (deg < 0.0) {
				deg += 360;
			}
			if (45 * 3 <= deg && deg < 45 * 7) {
				mTextRot += Math.PI;
			}
			int on, ov;
			if (0 == deg) {
				on = 12;
				ov = -11;
			} else if (0 < deg && deg < 45 * 3) {
				on = 10;
				ov = -13;
			} else if (45 * 3 <= deg && deg < 180) {
				on = -10;
				ov = 14;
			} else if (180 <= deg && deg < 45 * 7) {
				on = -10;
				ov = 13;
			} else {
				on = 11;
				ov = -12;
			}
			InterpolationPost(ref mNamePos, 0.5, on);
			InterpolationPost(ref mValuePos, 0.5, ov);
		}

		public override void Draw(CustomGraphics g) {
			Draw2Leads();

			/* draw first lead and plate */
			FillPolygon(mPlate1);
			/* draw second lead and plate */
			FillPolygon(mPlate2);

			DrawName();
			DrawValue(TextUtils.Unit(Capacitance));

			UpdateDotCount();
			if (ConstructItem != this) {
				DrawCurrentA(mCurCount);
				DrawCurrentB(mCurCount);
			}
		}

		public override void GetInfo(string[] arr) {
			if (string.IsNullOrEmpty(ReferenceName)) {
				arr[0] = "コンデンサ：" + TextUtils.Unit(Capacitance, "F");
				GetBasicInfo(1, arr);
			} else {
				arr[0] = ReferenceName;
				arr[1] = "コンデンサ：" + TextUtils.Unit(Capacitance, "F");
				GetBasicInfo(2, arr);
			}
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("キャパシタンス(F)", Capacitance);
			}
			if (r == 1) {
				return new ElementInfo("名前", ReferenceName);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0 && ei.Value > 0) {
				Capacitance = ei.Value;
				mLastValue = ei.Value;
				SetTextPos();
			}
			if (n == 1) {
				ReferenceName = ei.Text;
				mLastReferenceName = ReferenceName;
				SetTextPos();
			}
		}

		public override EventHandler CreateSlider(ElementInfo ei, Slider adj) {
			return new EventHandler((s, e) => {
				var trb = adj.Trackbar;
				Capacitance = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
				MainForm.MainForm.NeedAnalyze = true;
			});
		}
	}
}
