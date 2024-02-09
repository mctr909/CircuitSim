using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.Symbol.Active {
	class Transistor : BaseSymbol {
		const int FLAG_FLIP = 1;

		const int BODY_LEN = 13;
		const int HS = 12;
		const int BASE_THICK = 2;

		ElmTransistor mElm;

		double mCurCountC;
		double mCurCountE;
		double mCurCountB;

		PointF mTbase;

		PointF[] mRectPoly;
		PointF[] mArrowPoly;
		PointF[] mPosC = new PointF[3];
		PointF[] mPosE = new PointF[3];

		public override BaseElement Element { get { return mElm; } }

		public Transistor(Point pos, bool pnpFlag) : base(pos) {
			mElm = new ElmTransistor(pnpFlag);
			ReferenceName = "Tr";
			Setup();
		}

		public Transistor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			var npn = st.nextTokenInt(1);
			var vbe = st.nextTokenDouble();
			var vbc = st.nextTokenDouble();
			var hfe = st.nextTokenDouble(100);
			mElm = new ElmTransistor(npn, hfe, vbe, vbc);
			Setup();
		}

		public override bool CanViewInScope { get { return true; } }

		public override DUMP_ID DumpId { get { return DUMP_ID.TRANSISTOR; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.NPN);
			optionList.Add((mElm.Vb - mElm.Vc).ToString("g3"));
			optionList.Add((mElm.Vb - mElm.Ve).ToString("g3"));
			optionList.Add(mElm.Hfe);
		}

		void Setup() {
			mElm.Setup();
			Post.NoDiagonal = true;
		}

		void SetTextPos() {
			var swap = 0 < (mFlags & FLAG_FLIP) ? -1 : 1;
			if (Post.Horizontal) {
				if (0 < Post.Dsign * swap) {
					mNamePos = new Point(Post.B.X + 10, Post.B.Y);
				} else {
					mNamePos = new Point(Post.B.X - 6, Post.B.Y);
				}
			} else if (Post.Vertical) {
				if (0 < Post.Dsign) {
					mNamePos = new Point(Post.B.X, Post.B.Y + 15 * swap * 2 / 3);
				} else {
					mNamePos = new Point(Post.B.X, Post.B.Y - 13 * swap * 2 / 3);
				}
			} else {
				interpPost(ref mNamePos, 0.5, 10 * Post.Dsign);
			}
		}

		public override void SetPoints() {
			base.SetPoints();

			if ((mFlags & FLAG_FLIP) != 0) {
				Post.Dsign = -Post.Dsign;
			}

			/* calc collector, emitter posts */
			var hsm = (HS / 8 + 1) * 8;
			var hs1 = (HS - 2) * Post.Dsign * mElm.NPN;
			var hs2 = hsm * Post.Dsign * mElm.NPN;
			interpPostAB(ref mPosC[1], ref mPosE[1], 1, hs1);
			interpPostAB(ref mPosC[2], ref mPosE[2], 1, hs2);

			/* calc rectangle edges */
			var rect = new PointF[4];
			interpPostAB(ref rect[0], ref rect[1], 1 - BODY_LEN / Post.Len, HS);
			interpPostAB(ref rect[2], ref rect[3], 1 - (BODY_LEN - BASE_THICK) / Post.Len, HS);

			/* calc points where collector/emitter leads contact rectangle */
			interpPostAB(ref mPosC[0], ref mPosE[0],
				1 - (BODY_LEN - BASE_THICK * 0.5) / Post.Len,
				5 * Post.Dsign * mElm.NPN
			);

			/* calc point where base lead contacts rectangle */
			if (Post.Dsign < 0) {
				interpPost(ref mTbase, 1 - (BODY_LEN - BASE_THICK) / Post.Len);
			} else {
				interpPost(ref mTbase, 1 - BODY_LEN / Post.Len);
			}

			/* rectangle */
			mRectPoly = new PointF[] { rect[0], rect[2], rect[3], rect[1] };

			/* arrow */
			if (mElm.NPN == 1) {
				Utils.CreateArrow(mPosE[0], mPosE[1], out mArrowPoly, 8, 3);
			} else {
				var b = new PointF();
				interpPost(ref b, 1 - (BODY_LEN - 1) / Post.Len, -5 * Post.Dsign * mElm.NPN);
				Utils.CreateArrow(mPosE[1], b, out mArrowPoly, 8, 3);
			}
			SetTextPos();

			mElm.SetNodePos(Post.A, mPosC[2], mPosE[2]);
		}

		public override void Draw(CustomGraphics g) {
			/* draw collector */
			drawLine(mPosC[2], mPosC[1]);
			drawLine(mPosC[1], mPosC[0]);
			/* draw emitter */
			drawLine(mPosE[2], mPosE[1]);
			drawLine(mPosE[1], mPosE[0]);
			/* draw arrow */
			fillPolygon(mArrowPoly);
			/* draw base */
			drawLine(Post.A, mTbase);
			/* draw base rectangle */
			fillPolygon(mRectPoly);

			/* draw dots */
			updateDotCount(-mElm.Ib, ref mCurCountB);
			updateDotCount(-mElm.Ic, ref mCurCountC);
			updateDotCount(-mElm.Ie, ref mCurCountE);
			drawCurrent(mTbase, Post.A, mCurCountB);
			if (0 <= mElm.NPN * mElm.Ic) {
				drawCurrent(mPosE[1], mTbase, mCurCountB);
			} else {
				drawCurrent(mPosC[1], mTbase, mCurCountB);
			}
			drawCurrent(mPosE[1], mPosC[1], mCurCountC);

			if (ControlPanel.ChkShowName.Checked) {
				if (Post.Vertical) {
					drawCenteredText(ReferenceName, mNamePos);
				} else {
					drawCenteredText(ReferenceName, mNamePos, -Math.PI / 2);
				}
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = ((mElm.NPN == -1) ? "PNP" : "NPN") + "トランジスタ(" + "hfe：" + Utils.UnitText(mElm.Hfe) + ")";
			var vbc = mElm.Vb - mElm.Vc;
			var vbe = mElm.Vb - mElm.Ve;
			var vce = mElm.Vc - mElm.Ve;
			if (vbc * mElm.NPN > 0.2) {
				arr[1] = "動作領域：" + (vbe * mElm.NPN > 0.2 ? "飽和" : "逆流");
			} else {
				arr[1] = "動作領域：" + (vbe * mElm.NPN > 0.2 ? "活性" : "遮断");
			}
			arr[2] = "Vce：" + Utils.VoltageText(vce);
			arr[3] = "Vbe：" + Utils.VoltageText(vbe);
			arr[4] = "Vbc：" + Utils.VoltageText(vbc);
			arr[5] = "Ic：" + Utils.CurrentText(mElm.Ic);
			arr[6] = "Ib：" + Utils.CurrentText(mElm.Ib);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("名前", ReferenceName);
			}
			if (r == 1) {
				return new ElementInfo("hfe", mElm.Hfe);
			}
			if (r == 2) {
				return new ElementInfo("エミッタ/コレクタ 入れ替え", (mFlags & FLAG_FLIP) != 0);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				ReferenceName = ei.Text;
				SetTextPos();
			}
			if (n == 1) {
				mElm.Hfe = ei.Value;
				Setup();
			}
			if (n == 2) {
				if (ei.CheckBox.Checked) {
					mFlags |= FLAG_FLIP;
				} else {
					mFlags &= ~FLAG_FLIP;
				}
				SetPoints();
			}
		}
	}
}
