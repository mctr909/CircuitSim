using Circuit.Elements.Active;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Active {
	class Diode : BaseSymbol {
		public const int FLAG_FWDROP = 1;
		public const int FLAG_MODEL = 2;
		protected const float HS = 5.5f;
		protected int BODY_LEN = 9;

		protected PointF[] mPoly;
		protected PointF[] mCathode;

		protected List<DiodeModel> mModels;
		protected bool mCustomModelUI;
		protected bool mHasResistance;

		public static string LastModelName = "default";
		public string ModelName = "default";
		public DiodeModel Model;

		protected ElmDiode mElm;

		public override int InternalNodeCount { get { return mHasResistance ? 1 : 0; } }

		public Diode(Point pos, string referenceName = "D") : base(pos) {
			ModelName = LastModelName;
			ReferenceName = referenceName;
			var model = DiodeModel.GetModelWithName(ModelName);
			mElm = (ElmDiode)Element;
			mElm.VZener = model.BreakdownVoltage;
			mElm.FwDrop = model.FwDrop;
			mElm.Leakage = model.SaturationCurrent;
			mElm.VScale = model.VScale;
			mElm.VdCoef = model.VdCoef;
			mElm.SeriesResistance = model.SeriesResistance;
			Model = model;
			Setup();
		}

		public Diode(Point p1, Point p2, int f) : base(p1, p2, f) {
			mElm = (ElmDiode)Element;
		}

		public Diode(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			const double defaultdrop = 0.805904783;
			double fwdrop = defaultdrop;
			double zvoltage = 0;
			if (0 != (f & FLAG_MODEL)) {
				if (st.nextToken(out ModelName, LastModelName)) {
					ModelName = TextUtils.UnEscape(ModelName);
				}
			} else {
				if (0 != (f & FLAG_FWDROP)) {
					fwdrop = st.nextTokenDouble();
				}
				ModelName = DiodeModel.GetModelWithParameters(fwdrop, zvoltage).Name;
			}
			var model = DiodeModel.GetModelWithName(ModelName);
			mElm = (ElmDiode)Element;
			mElm.VZener = model.BreakdownVoltage;
			mElm.FwDrop = model.FwDrop;
			mElm.Leakage = model.SaturationCurrent;
			mElm.VScale = model.VScale;
			mElm.VdCoef = model.VdCoef;
			mElm.SeriesResistance = model.SeriesResistance;
			Model = model;
			Setup();
		}

		protected override BaseElement Create() {
			return new ElmDiode();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.DIODE; } }

		protected override void dump(List<object> optionList) {
			mFlags |= FLAG_MODEL;
			optionList.Add(TextUtils.Escape(ModelName));
		}

		public override void Stamp() {
			if (mHasResistance) {
				/* create diode from node 0 to internal node */
				mElm.mNodes0 = mElm.Nodes[0];
				mElm.mNodes1 = mElm.Nodes[2];
				/* create resistor from internal node to node 1 */
				StampResistor(mElm.Nodes[1], mElm.Nodes[2], mElm.SeriesResistance);
			} else {
				/* don't need any internal nodes if no series resistance */
				mElm.mNodes0 = mElm.Nodes[0];
				mElm.mNodes1 = mElm.Nodes[1];
			}
			StampNonLinear(mElm.mNodes0);
			StampNonLinear(mElm.mNodes1);
		}

		public void Setup() {
			/* critical voltage for limiting; current is vscale/sqrt(2) at this voltage */
			mElm.mVCrit = mElm.VScale * Math.Log(mElm.VScale / (Math.Sqrt(2) * mElm.Leakage));
			/* translated, *positive* critical voltage for limiting in Zener breakdown region;
             * limitstep() uses this with translated voltages in an analogous fashion to vcrit. */
			mElm.mVzCrit = ElmDiode.VTH * Math.Log(ElmDiode.VTH / (Math.Sqrt(2) * mElm.Leakage));
			if (mElm.VZener == 0) {
				mElm.mVzOffset = 0;
			} else {
				/* calculate offset which will give us 5mA at zvoltage */
				double i = -0.005;
				mElm.mVzOffset = mElm.VZener - Math.Log(-(1 + i / mElm.Leakage)) / ElmDiode.VZ_COEF;
			}
			mHasResistance = 0 < mElm.SeriesResistance;
			mElm.mDiodeEndNode = mHasResistance ? 2 : 1;
			AllocateNodes();
		}

		public override void Reset() {
			mElm.mLastVoltDiff = 0;
			mElm.V[0] = mElm.V[1] = 0;
			if (mHasResistance) {
				mElm.V[2] = 0;
			}
		}

		public override void SetPoints() {
			base.SetPoints();
			SetLeads(BODY_LEN);
			mCathode = new PointF[4];
			InterpolationLeadAB(ref mCathode[0], ref mCathode[1], (BODY_LEN - 0.75) / BODY_LEN, HS);
			InterpolationLeadAB(ref mCathode[3], ref mCathode[2], (BODY_LEN + 0.75) / BODY_LEN, HS);
			var pa = new PointF[2];
			InterpolationLeadAB(ref pa[0], ref pa[1], -1.0 / BODY_LEN, HS);
			mPoly = new PointF[] { pa[0], pa[1], mLead2 };
			SetTextPos();
		}

		protected void SetTextPos() {
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
			if (0 < deg && deg < 45 * 3) {
				InterpolationPost(ref mValuePos, 0.5, 12 * Post.Dsign);
				InterpolationPost(ref mNamePos, 0.5, -10 * Post.Dsign);
			} else if (45 * 3 <= deg && deg <= 180) {
				InterpolationPost(ref mNamePos, 0.5, 10 * Post.Dsign);
				InterpolationPost(ref mValuePos, 0.5, -14 * Post.Dsign);
			} else if (180 < deg && deg < 45 * 7) {
				InterpolationPost(ref mNamePos, 0.5, -10 * Post.Dsign);
				InterpolationPost(ref mValuePos, 0.5, 12 * Post.Dsign);
			} else {
				InterpolationPost(ref mNamePos, 0.5, 12 * Post.Dsign);
				InterpolationPost(ref mValuePos, 0.5, -12 * Post.Dsign);
			}
		}

		public override void Draw(CustomGraphics g) {
			DrawDiode();
			DoDots();
			DrawName();
		}

		protected void DrawDiode() {
			Draw2Leads();
			/* draw arrow thingy */
			FillPolygon(mPoly);
			/* draw thing arrow is pointing to */
			if (mCathode.Length < 4) {
				DrawLine(mCathode[0], mCathode[1]);
			} else {
				FillPolygon(mCathode);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "ダイオード";
			GetBasicInfo(1, arr);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("名前", ReferenceName);
			}
			if (!mCustomModelUI && r == 1) {
				var ei = new ElementInfo("モデル");
				mModels = DiodeModel.GetModelList(this is DiodeZener);
				ei.Choice = new ComboBox();
				for (int i = 0; i != mModels.Count; i++) {
					var dm = mModels[i];
					ei.Choice.Items.Add(dm.GetDescription());
					if (dm == Model) {
						ei.Choice.SelectedIndex = i;
					}
				}
				return ei;
			}
			return base.GetElementInfo(r, c);
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (!mCustomModelUI && n == 1) {
				int ix = ei.Choice.SelectedIndex;
				if (ix >= mModels.Count) {
					mModels = null;
					mCustomModelUI = true;
					ei.NewDialog = true;
					return;
				}
				ModelName = mModels[ei.Choice.SelectedIndex].Name;
				var model = DiodeModel.GetModelWithName(ModelName);
				mElm.VZener = model.BreakdownVoltage;
				mElm.FwDrop = model.FwDrop;
				mElm.Leakage = model.SaturationCurrent;
				mElm.VScale = model.VScale;
				mElm.VdCoef = model.VdCoef;
				mElm.SeriesResistance = model.SeriesResistance;
				Model = model;
				Setup();
				return;
			}
			base.SetElementValue(n, c, ei);
			if (n == 0) {
				ReferenceName = ei.Text;
				SetTextPos();
			}
		}
	}
}
