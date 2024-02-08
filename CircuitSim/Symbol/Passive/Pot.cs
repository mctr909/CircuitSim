using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Passive;

namespace Circuit.Symbol.Passive {
	class Pot : BaseSymbol {
		const int FLAG_SHOW_VALUES = 1;

		const int HS = 5;
		const int BODY_LEN = 24;
		const int SEGMENTS = 12;
		const double SEG_F = 1.0 / SEGMENTS;

		PointF mTermA;
		PointF mTermB;
		PointF mTermSlider;

		PointF mCorner2;
		PointF mArrowPoint;
		PointF mMidPoint;
		PointF mArrow1;
		PointF mArrow2;
		PointF[] mPs1;
		PointF[] mPs2;
		PointF[] mRect1;
		PointF[] mRect2;
		PointF[] mRect3;
		PointF[] mRect4;

		ElmPot mElm;
		TrackBar mSlider;
		Label mLabel;
		string mName;

		public Pot(Point pos) : base(pos) {
			mElm = new ElmPot();
			Elm = mElm;
			mElm.AllocNodes();
			mFlags = FLAG_SHOW_VALUES;
			ReferenceName = "VR";
			CreateSlider();
		}

		public Pot(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmPot();
			Elm = mElm;
			mElm.MaxResistance = st.nextTokenDouble(1e3);
			mElm.Position = st.nextTokenDouble(0.5);
			mElm.AllocNodes();
			CreateSlider();
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.POT; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.MaxResistance.ToString("g3"));
			optionList.Add(mElm.Position);
		}

		public override void Delete() {
			ControlPanel.RemoveSlider(mLabel);
			ControlPanel.RemoveSlider(mSlider);
			base.Delete();
		}

		public override double Distance(Point p) {
			return Math.Min(
				Utils.DistanceOnLine(mTermA, mTermB, p), Math.Min(
				Utils.DistanceOnLine(mArrowPoint, mCorner2, p),
				Utils.DistanceOnLine(mCorner2, mTermSlider, p)
			));
		}

		public override void SetPoints() {
			base.SetPoints();
			Post.Vertical = Math.Abs(Post.Diff.X) <= Math.Abs(Post.Diff.Y);
			Post.Horizontal = !Post.Vertical;

			mTermA = Post.A;
			mTermB = Post.B;

			int offset = 0;
			if (Post.Vertical) {
				/* vertical */
				var myLen = 2 * CirSimForm.GRID_SIZE * Math.Sign(Post.Diff.Y)
					* ((Math.Abs(Post.Diff.Y) + 2 * CirSimForm.GRID_SIZE - 1) / (2 * CirSimForm.GRID_SIZE));
				if (Post.Diff.Y != 0) {
					mTermB.X = mTermA.X;
					mTermB.Y = mTermA.Y + myLen;
					offset = (0 < Post.Diff.Y) ? Post.Diff.X : -Post.Diff.X;
				}
			} else {
				/* horizontal */
				var myLen = 2 * CirSimForm.GRID_SIZE * Math.Sign(Post.Diff.X)
					* ((Math.Abs(Post.Diff.X) + 2 * CirSimForm.GRID_SIZE - 1) / (2 * CirSimForm.GRID_SIZE));
				mTermB.X = mTermA.X + myLen;
				mTermB.Y = mTermA.Y;
				offset = (Post.Diff.X < 0) ? Post.Diff.Y : -Post.Diff.Y;
			}
			if (offset < CirSimForm.GRID_SIZE) {
				offset = CirSimForm.GRID_SIZE;
			}
			Post.Len = Utils.Distance(mTermA, mTermB);

			Utils.InterpPoint(mTermA, mTermB, out mLead1, (Post.Len - BODY_LEN) / (2 * Post.Len));
			Utils.InterpPoint(mTermA, mTermB, out mLead2, (Post.Len + BODY_LEN) / (2 * Post.Len));

			/* set slider */
			mElm.Position = mSlider.Value * 0.0099 + 0.0001;
			var poff = 0.5;
			var woff = -7.0;
			int soff = (int)((mElm.Position - poff) * BODY_LEN);
			Utils.InterpPoint(mTermA, mTermB, out mTermSlider, poff, offset);
			Utils.InterpPoint(mTermA, mTermB, out mCorner2, soff / Post.Len + poff, offset);
			Utils.InterpPoint(mTermA, mTermB, out mArrowPoint, soff / Post.Len + poff, 7 * Math.Sign(offset));
			Utils.InterpPoint(mTermA, mTermB, out mMidPoint, soff / Post.Len + poff);

			var clen = Math.Abs(offset) + woff;
			Utils.InterpPoint(mCorner2, mArrowPoint, out mArrow1, out mArrow2, (clen + woff) / clen, 4);

			SetPoly();
			SetTextPos();

			mElm.SetNodePos(mTermA, mTermB, mTermSlider);
		}

		public override void Draw(CustomGraphics g) {
			drawLine(mTermA, mLead1);
			drawLine(mLead2, mTermB);

			if (ControlPanel.ChkUseAnsiSymbols.Checked) {
				/* draw zigzag */
				for (int i = 0; i != SEGMENTS; i++) {
					drawLine(mPs1[i], mPs2[i]);
				}
			} else {
				/* draw rectangle */
				drawLine(mRect1[0], mRect2[0]);
				for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
					drawLine(mRect1[j], mRect3[j]);
					drawLine(mRect2[j], mRect4[j]);
				}
				drawLine(mRect1[SEGMENTS + 1], mRect2[SEGMENTS + 1]);
			}

			/* draw slider */
			drawLine(mTermSlider, mCorner2);
			drawLine(mCorner2, mArrowPoint);
			drawLine(mArrow1, mArrowPoint);
			drawLine(mArrow2, mArrowPoint);

			/* draw dot */
			updateDotCount(mElm.Current1, ref mElm.CurCount1);
			updateDotCount(mElm.Current2, ref mElm.CurCount2);
			updateDotCount(mElm.Current3, ref mElm.CurCount3);
			if (CirSimForm.ConstructElm != this) {
				drawCurrent(mTermA, mMidPoint, mElm.CurCount1);
				drawCurrent(mTermB, mMidPoint, mElm.CurCount2);
				drawCurrent(mTermSlider, mCorner2, mElm.CurCount3);
				drawCurrent(mCorner2, mMidPoint, mElm.CurCount3 + Utils.Distance(mTermSlider, mCorner2));
			}

			if (ControlPanel.ChkShowValues.Checked && mElm.Resistance1 > 0 && (mFlags & FLAG_SHOW_VALUES) != 0) {
				/* check for vertical pot with 3rd terminal on left */
				bool reverseY = (mTermSlider.X < mLead1.X && mLead1.X == mLead2.X);
				/* check for horizontal pot with 3rd terminal on top */
				bool reverseX = (mTermSlider.Y < mLead1.Y && mLead1.X != mLead2.X);
				/* check if we need to swap texts (if leads are reversed, e.g. drawn right to left) */
				bool rev = (mLead1.X == mLead2.X && mLead1.Y < mLead2.Y) || (mLead1.Y == mLead2.Y && mLead1.X > mLead2.X);

				/* draw units */
				var s1 = Utils.UnitText(rev ? mElm.Resistance2 : mElm.Resistance1, "");
				var s2 = Utils.UnitText(rev ? mElm.Resistance1 : mElm.Resistance2, "");
				var txtHeightHalf = g.FontSize * 0.5f;
				var txtWidth1 = (int)g.GetTextSize(s1).Width;
				var txtWidth2 = (int)g.GetTextSize(s2).Width;

				if (Post.Horizontal) {
					var y = (int)(mArrowPoint.Y + (reverseX ? -txtHeightHalf : txtHeightHalf));
					drawLeftText(s1, Math.Min(mArrow1.X, mArrow2.X) - txtWidth1, y);
					drawLeftText(s2, Math.Max(mArrow1.X, mArrow2.X), y);
				} else {
					drawLeftText(s1, reverseY ? (mArrowPoint.X - txtWidth1) : mArrowPoint.X, (int)(Math.Min(mArrow1.Y, mArrow2.Y) + txtHeightHalf * 3));
					drawLeftText(s2, reverseY ? (mArrowPoint.X - txtWidth2) : mArrowPoint.X, (int)(Math.Max(mArrow1.Y, mArrow2.Y) - txtHeightHalf * 3));
				}
			}
			if (Post.Vertical) {
				drawCenteredText(mName, mNamePos, -Math.PI / 2);
			} else {
				drawCenteredText(mName, mNamePos);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "可変抵抗：" + Utils.UnitText(mElm.MaxResistance, CirSimForm.OHM_TEXT);
			arr[1] = "Vd：" + Utils.VoltageAbsText(mElm.VoltageDiff);
			arr[2] = "R1：" + Utils.UnitText(mElm.Resistance1, CirSimForm.OHM_TEXT);
			arr[3] = "R2：" + Utils.UnitText(mElm.Resistance2, CirSimForm.OHM_TEXT);
			arr[4] = "I1：" + Utils.CurrentAbsText(mElm.Current1);
			arr[5] = "I2：" + Utils.CurrentAbsText(mElm.Current2);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("レジスタンス(Ω)", mElm.MaxResistance);
			}
			if (r == 1) {
				return new ElementInfo("名前", ReferenceName);
			}
			if (r == 2) {
				return new ElementInfo("値を表示", (mFlags & FLAG_SHOW_VALUES) != 0);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				mElm.MaxResistance = ei.Value;
			}
			if (n == 1) {
				ReferenceName = ei.Text;
				mLabel.Text = ReferenceName;
				ControlPanel.SetSliderPanelHeight();
			}
			if (n == 2) {
				mFlags = ei.ChangeFlag(mFlags, FLAG_SHOW_VALUES);
			}
			SetTextPos();
		}

		void SetPoly() {
			/* set zigzag */
			int oy = 0;
			int ny;
			mPs1 = new PointF[SEGMENTS + 1];
			mPs2 = new PointF[SEGMENTS + 1];
			for (int i = 0; i != SEGMENTS; i++) {
				switch (i & 3) {
				case 0:
					ny = HS;
					break;
				case 2:
					ny = -HS;
					break;
				default:
					ny = 0;
					break;
				}
				interpLead(ref mPs1[i], i * SEG_F, oy);
				interpLead(ref mPs2[i], (i + 1) * SEG_F, ny);
				oy = ny;
			}

			/* set rectangle */
			mRect1 = new PointF[SEGMENTS + 2];
			mRect2 = new PointF[SEGMENTS + 2];
			mRect3 = new PointF[SEGMENTS + 2];
			mRect4 = new PointF[SEGMENTS + 2];
			Utils.InterpPoint(mTermA, mTermB, out mRect1[0], out mRect2[0], 0, HS);
			for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
				Utils.InterpPoint(mTermA, mTermB, out mRect1[j], out mRect2[j], i * SEG_F, HS);
				Utils.InterpPoint(mTermA, mTermB, out mRect3[j], out mRect4[j], (i + 1) * SEG_F, HS);
			}
			Utils.InterpPoint(mTermA, mTermB, out mRect1[SEGMENTS + 1], out mRect2[SEGMENTS + 1], 1, HS);
		}

		void SetTextPos() {
			mName = "";
			if (ControlPanel.ChkShowName.Checked) {
				mName += ReferenceName;
			}
			if (ControlPanel.ChkShowValues.Checked) {
				if (!string.IsNullOrEmpty(mName)) {
					mName += " ";
				}
				mName += Utils.UnitText(mElm.MaxResistance);
			}
			if (Post.Horizontal) {
				if (0 < Post.Diff.Y) {
					/* right slider */
					Utils.InterpPoint(mTermA, mTermB, out mNamePos, 0.5, -12 * Post.Dsign);
				} else {
					/* left slider */
					Utils.InterpPoint(mTermA, mTermB, out mNamePos, 0.5, 12 * Post.Dsign);
				}
			} else {
				if (0 < Post.Diff.X) {
					/* upper slider */
					Utils.InterpPoint(mTermA, mTermB, out mNamePos, 0.5, -9 * Post.Dsign);
				} else {
					/* lower slider */
					Utils.InterpPoint(mTermA, mTermB, out mNamePos, 0.5, 13 * Post.Dsign);
				}
			}
		}

		void CreateSlider() {
			ControlPanel.AddSlider(mLabel = new Label()
			{
				TextAlign = ContentAlignment.BottomLeft,
				Text = ReferenceName
			});
			int value = (int)(mElm.Position * 100);
			ControlPanel.AddSlider(mSlider = new TrackBar()
			{
				Minimum = 0,
				Maximum = 100,
				SmallChange = 1,
				LargeChange = 5,
				TickFrequency = 10,
				Value = value,
				Width = 175
			});
			mSlider.ValueChanged += new EventHandler((s, e) => {
				SetPoints();
				CirSimForm.NeedAnalyze();
			});
		}
	}
}
