﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Active;

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

		protected ElmDiode mElm;

		public Diode(Point pos, string referenceName = "D") : base(pos) {
			mElm = new ElmDiode();
			Elm = mElm;
			ReferenceName = referenceName;
			mElm.Setup();
		}

		public Diode(Point p1, Point p2, int f) : base(p1, p2, f) { }

		public Diode(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmDiode(st, 0 != (f & FLAG_FWDROP), 0 != (f & FLAG_MODEL));
			Elm = mElm;
			mElm.Setup();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.DIODE; } }

		protected override void dump(List<object> optionList) {
			mFlags |= FLAG_MODEL;
			optionList.Add(Utils.Escape(mElm.ModelName));
		}

		public override void SetPoints() {
			base.SetPoints();
			setLeads(BODY_LEN);
			mCathode = new PointF[4];
			interpLeadAB(ref mCathode[0], ref mCathode[1], (BODY_LEN - 1.0) / BODY_LEN, HS);
			interpLeadAB(ref mCathode[3], ref mCathode[2], (BODY_LEN + 1.0) / BODY_LEN, HS);
			var pa = new PointF[2];
			interpLeadAB(ref pa[0], ref pa[1], -1.0 / BODY_LEN, HS);
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
				interpPost(ref mValuePos, 0.5, 12 * Post.Dsign);
				interpPost(ref mNamePos, 0.5, -10 * Post.Dsign);
			} else if (45 * 3 <= deg && deg <= 180) {
				interpPost(ref mNamePos, 0.5, 10 * Post.Dsign);
				interpPost(ref mValuePos, 0.5, -14 * Post.Dsign);
			} else if (180 < deg && deg < 45 * 7) {
				interpPost(ref mNamePos, 0.5, -10 * Post.Dsign);
				interpPost(ref mValuePos, 0.5, 12 * Post.Dsign);
			} else {
				interpPost(ref mNamePos, 0.5, 12 * Post.Dsign);
				interpPost(ref mValuePos, 0.5, -12 * Post.Dsign);
			}
		}

		public override void Draw(CustomGraphics g) {
			DrawDiode();
			doDots();
			drawName();
		}

		protected void DrawDiode() {
			draw2Leads();
			/* draw arrow thingy */
			fillPolygon(mPoly);
			/* draw thing arrow is pointing to */
			if (mCathode.Length < 4) {
				drawLine(mCathode[0], mCathode[1]);
			} else {
				fillPolygon(mCathode);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "ダイオード";
			getBasicInfo(1, arr);
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
					if (dm == mElm.Model) {
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
				mElm.Model = mModels[ei.Choice.SelectedIndex];
				mElm.ModelName = mElm.Model.Name;
				mElm.Setup();
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