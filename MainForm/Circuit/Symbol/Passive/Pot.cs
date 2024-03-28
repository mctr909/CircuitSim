using Circuit.Forms;
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

		double mCurCount1 = 0;
		double mCurCount2 = 0;
		double mCurCount3 = 0;

		public override BaseElement Element { get { return mElm; } }

		public Pot(Point pos) : base(pos) {
			mElm = new ElmPot();
			mElm.alloc_nodes();
			mFlags = FLAG_SHOW_VALUES;
			ReferenceName = "VR";
			CreateSlider();
		}

		public Pot(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			mElm = new ElmPot() {
				MaxResistance = st.nextTokenDouble(1e3),
				Position = st.nextTokenDouble(0.5)
			};
			mElm.alloc_nodes();
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
				Post.DistanceOnLine(mTermA, mTermB, p), Math.Min(
				Post.DistanceOnLine(mArrowPoint, mCorner2, p),
				Post.DistanceOnLine(mCorner2, mTermSlider, p)
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
				var myLen = 2 * GRID_SIZE * Math.Sign(Post.Diff.Y)
					* ((Math.Abs(Post.Diff.Y) + 2 * GRID_SIZE - 1) / (2 * GRID_SIZE));
				if (Post.Diff.Y != 0) {
					mTermB.X = mTermA.X;
					mTermB.Y = mTermA.Y + myLen;
					offset = (0 < Post.Diff.Y) ? Post.Diff.X : -Post.Diff.X;
				}
			} else {
				/* horizontal */
				var myLen = 2 * GRID_SIZE * Math.Sign(Post.Diff.X)
					* ((Math.Abs(Post.Diff.X) + 2 * GRID_SIZE - 1) / (2 * GRID_SIZE));
				mTermB.X = mTermA.X + myLen;
				mTermB.Y = mTermA.Y;
				offset = (Post.Diff.X < 0) ? Post.Diff.Y : -Post.Diff.Y;
			}
			if (offset < GRID_SIZE) {
				offset = GRID_SIZE;
			}
			Post.Len = Distance(mTermA, mTermB);

			InterpolationPoint(mTermA, mTermB, out mLead1, (Post.Len - BODY_LEN) / (2 * Post.Len));
			InterpolationPoint(mTermA, mTermB, out mLead2, (Post.Len + BODY_LEN) / (2 * Post.Len));

			/* set slider */
			mElm.Position = mSlider.Value * 0.0099 + 0.0001;
			var poff = 0.5;
			var woff = -7.0;
			int soff = (int)((mElm.Position - poff) * BODY_LEN);
			InterpolationPoint(mTermA, mTermB, out mTermSlider, poff, offset);
			InterpolationPoint(mTermA, mTermB, out mCorner2, soff / Post.Len + poff, offset);
			InterpolationPoint(mTermA, mTermB, out mArrowPoint, soff / Post.Len + poff, 7 * Math.Sign(offset));
			InterpolationPoint(mTermA, mTermB, out mMidPoint, soff / Post.Len + poff);

			var clen = Math.Abs(offset) + woff;
			InterpolationPoint(mCorner2, mArrowPoint, out mArrow1, out mArrow2, (clen + woff) / clen, 4);

			SetPoly();
			SetTextPos();

			mElm.set_node_pos(mTermA, mTermB, mTermSlider);
		}

		public override void Draw(CustomGraphics g) {
			DrawLine(mTermA, mLead1);
			DrawLine(mLead2, mTermB);

			if (ControlPanel.ChkUseAnsiSymbols.Checked) {
				/* draw zigzag */
				for (int i = 0; i != SEGMENTS; i++) {
					DrawLine(mPs1[i], mPs2[i]);
				}
			} else {
				/* draw rectangle */
				DrawLine(mRect1[0], mRect2[0]);
				for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
					DrawLine(mRect1[j], mRect3[j]);
					DrawLine(mRect2[j], mRect4[j]);
				}
				DrawLine(mRect1[SEGMENTS + 1], mRect2[SEGMENTS + 1]);
			}

			/* draw slider */
			DrawLine(mTermSlider, mCorner2);
			DrawLine(mCorner2, mArrowPoint);
			DrawLine(mArrow1, mArrowPoint);
			DrawLine(mArrow2, mArrowPoint);

			/* draw dot */
			UpdateDotCount(mElm.Current1, ref mCurCount1);
			UpdateDotCount(mElm.Current2, ref mCurCount2);
			UpdateDotCount(mElm.Current3, ref mCurCount3);
			if (ConstructItem != this) {
				DrawCurrent(mTermA, mMidPoint, mCurCount1);
				DrawCurrent(mTermB, mMidPoint, mCurCount2);
				DrawCurrent(mTermSlider, mCorner2, mCurCount3);
				DrawCurrent(mCorner2, mMidPoint, mCurCount3 + Distance(mTermSlider, mCorner2));
			}

			if (ControlPanel.ChkShowValues.Checked && mElm.Resistance1 > 0 && (mFlags & FLAG_SHOW_VALUES) != 0) {
				/* check for vertical pot with 3rd terminal on left */
				bool reverseY = (mTermSlider.X < mLead1.X && mLead1.X == mLead2.X);
				/* check for horizontal pot with 3rd terminal on top */
				bool reverseX = (mTermSlider.Y < mLead1.Y && mLead1.X != mLead2.X);
				/* check if we need to swap texts (if leads are reversed, e.g. drawn right to left) */
				bool rev = (mLead1.X == mLead2.X && mLead1.Y < mLead2.Y) || (mLead1.Y == mLead2.Y && mLead1.X > mLead2.X);

				/* draw units */
				var s1 = TextUtils.Unit(rev ? mElm.Resistance2 : mElm.Resistance1, "");
				var s2 = TextUtils.Unit(rev ? mElm.Resistance1 : mElm.Resistance2, "");
				var txtHeightHalf = g.FontSize * 0.5f;
				var txtWidth1 = (int)g.GetTextSize(s1).Width;
				var txtWidth2 = (int)g.GetTextSize(s2).Width;

				if (Post.Horizontal) {
					var y = (int)(mArrowPoint.Y + (reverseX ? -txtHeightHalf : txtHeightHalf));
					DrawLeftText(s1, Math.Min(mArrow1.X, mArrow2.X) - txtWidth1, y);
					DrawLeftText(s2, Math.Max(mArrow1.X, mArrow2.X), y);
				} else {
					DrawLeftText(s1, reverseY ? (mArrowPoint.X - txtWidth1) : mArrowPoint.X, (int)(Math.Min(mArrow1.Y, mArrow2.Y) + txtHeightHalf * 3));
					DrawLeftText(s2, reverseY ? (mArrowPoint.X - txtWidth2) : mArrowPoint.X, (int)(Math.Max(mArrow1.Y, mArrow2.Y) - txtHeightHalf * 3));
				}
			}
			if (Post.Vertical) {
				DrawCenteredText(mName, mNamePos, -Math.PI / 2);
			} else {
				DrawCenteredText(mName, mNamePos);
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = "可変抵抗：" + TextUtils.Unit(mElm.MaxResistance, "Ω");
			arr[1] = "Vd：" + TextUtils.VoltageAbs(mElm.voltage_diff());
			arr[2] = "R1：" + TextUtils.Unit(mElm.Resistance1, "Ω");
			arr[3] = "R2：" + TextUtils.Unit(mElm.Resistance2, "Ω");
			arr[4] = "I1：" + TextUtils.CurrentAbs(mElm.Current1);
			arr[5] = "I2：" + TextUtils.CurrentAbs(mElm.Current2);
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
				InterpolationLead(ref mPs1[i], i * SEG_F, oy);
				InterpolationLead(ref mPs2[i], (i + 1) * SEG_F, ny);
				oy = ny;
			}

			/* set rectangle */
			mRect1 = new PointF[SEGMENTS + 2];
			mRect2 = new PointF[SEGMENTS + 2];
			mRect3 = new PointF[SEGMENTS + 2];
			mRect4 = new PointF[SEGMENTS + 2];
			InterpolationPoint(mTermA, mTermB, out mRect1[0], out mRect2[0], 0, HS);
			for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
				InterpolationPoint(mTermA, mTermB, out mRect1[j], out mRect2[j], i * SEG_F, HS);
				InterpolationPoint(mTermA, mTermB, out mRect3[j], out mRect4[j], (i + 1) * SEG_F, HS);
			}
			InterpolationPoint(mTermA, mTermB, out mRect1[SEGMENTS + 1], out mRect2[SEGMENTS + 1], 1, HS);
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
				mName += TextUtils.Unit(mElm.MaxResistance);
			}
			if (Post.Horizontal) {
				if (0 < Post.Diff.Y) {
					/* right slider */
					InterpolationPoint(mTermA, mTermB, out mNamePos, 0.5, -12 * Post.Dsign);
				} else {
					/* left slider */
					InterpolationPoint(mTermA, mTermB, out mNamePos, 0.5, 12 * Post.Dsign);
				}
			} else {
				if (0 < Post.Diff.X) {
					/* upper slider */
					InterpolationPoint(mTermA, mTermB, out mNamePos, 0.5, -9 * Post.Dsign);
				} else {
					/* lower slider */
					InterpolationPoint(mTermA, mTermB, out mNamePos, 0.5, 13 * Post.Dsign);
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
				CircuitSymbol.NeedAnalyze = true;
			});
		}
	}
}
