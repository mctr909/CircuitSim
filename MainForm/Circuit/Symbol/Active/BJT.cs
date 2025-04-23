using Circuit.Elements.Active;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Active {
	class BJT : BaseSymbol {
		const int FLAG_FLIP = 1;

		const int BODY_LEN = 13;
		const int HS = 12;
		const int BASE_THICK = 2;

		double HFE;

		double mCurCountC;
		double mCurCountE;
		double mCurCountB;

		PointF mTbase;

		PointF[] mRectPoly;
		PointF[] mArrowPoly;
		PointF[] mPosC = new PointF[3];
		PointF[] mPosE = new PointF[3];

		public Point[] Terminal { get {
			return [
				Post.A,
				new Point((int)mPosC[2].X, (int)mPosC[2].Y),
				new Point((int)mPosE[2].X, (int)mPosE[2].Y)
			];
		} }

		public BJT(Point pos, bool pnpFlag) : base(pos) {
			Element.Para[ElmBJT.NPN] = pnpFlag ? -1 : 1;
			Element.Para[ElmBJT.MAX_POW] = 1e6;
			ReferenceName = "Tr";
			Post.NoDiagonal = true;
			SetHfe(100);
		}

		public BJT(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
			var npn = st.nextTokenInt(1);
			var vbe = st.nextTokenDouble(0);
			var vbc = st.nextTokenDouble(0);
			var hfe = st.nextTokenDouble(100);
			var pow = st.nextTokenDouble(1e6);
			Element.Para[ElmBJT.NPN] = npn;
			Element.Para[ElmBJT.MAX_POW] = pow;
			Element.V[ElmBJT.C] = -vbe;
			Element.V[ElmBJT.E] = -vbc;
			Element.V[ElmBJT.BE] = vbe;
			Element.V[ElmBJT.BC] = vbc;
			Post.NoDiagonal = true;
			SetHfe(hfe);
		}

		protected override BaseElement Create() {
			return new ElmBJT();
		}

		public override bool CanViewInScope { get { return true; } }

		public override DUMP_ID DumpId { get { return DUMP_ID.TRANSISTOR; } }

		protected override void dump(List<object> optionList) {
			optionList.Add((int)Element.Para[ElmBJT.NPN]);
			optionList.Add((Element.V[ElmBJT.B] - Element.V[ElmBJT.C]).ToString("g3"));
			optionList.Add((Element.V[ElmBJT.B] - Element.V[ElmBJT.E]).ToString("g3"));
			optionList.Add(HFE);
			optionList.Add(Element.Para[ElmBJT.MAX_POW]);
		}

		public override void Reset() {
			Element.V[ElmBJT.B] = Element.V[ElmBJT.C] = Element.V[ElmBJT.E] = 0;
			Element.V[ElmBJT.BC] = Element.V[ElmBJT.BE] = 0;
		}

		public override void Stamp() {
			StampNonLinear(Element.Nodes[ElmBJT.B]);
			StampNonLinear(Element.Nodes[ElmBJT.C]);
			StampNonLinear(Element.Nodes[ElmBJT.E]);
		}

		void SetHfe(double hfe) {
			HFE = hfe;
			var gain = hfe / (hfe + 1.0);
			Element.Para[ElmBJT.GAIN] = gain;
			Element.Para[ElmBJT.GAIN_INV] = 1.0 / gain;
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
				InterpolationPost(ref mNamePos, 0.5, 10 * Post.Dsign);
			}
		}

		public override void SetPoints() {
			base.SetPoints();

			if ((mFlags & FLAG_FLIP) != 0) {
				Post.Dsign = -Post.Dsign;
			}

			/* calc collector, emitter posts */
			var hsm = (HS / 8 + 1) * 8;
			var hs1 = (HS - 2) * Post.Dsign * (int)Element.Para[ElmBJT.NPN];
			var hs2 = hsm * Post.Dsign * (int)Element.Para[ElmBJT.NPN];
			InterpolationPostAB(ref mPosC[1], ref mPosE[1], 1, hs1);
			InterpolationPostAB(ref mPosC[2], ref mPosE[2], 1, hs2);

			/* calc rectangle edges */
			var rect = new PointF[4];
			InterpolationPostAB(ref rect[0], ref rect[1], 1 - BODY_LEN / Post.Len, HS);
			InterpolationPostAB(ref rect[2], ref rect[3], 1 - (BODY_LEN - BASE_THICK) / Post.Len, HS);

			/* calc points where collector/emitter leads contact rectangle */
			InterpolationPostAB(ref mPosC[0], ref mPosE[0],
				1 - (BODY_LEN - BASE_THICK * 0.5) / Post.Len,
				5 * Post.Dsign * (int)Element.Para[ElmBJT.NPN]
			);

			/* calc point where base lead contacts rectangle */
			if (Post.Dsign < 0) {
				InterpolationPost(ref mTbase, 1 - (BODY_LEN - BASE_THICK) / Post.Len);
			} else {
				InterpolationPost(ref mTbase, 1 - BODY_LEN / Post.Len);
			}

			/* rectangle */
			mRectPoly = [rect[0], rect[2], rect[3], rect[1]];

			/* arrow */
			if (Element.Para[ElmBJT.NPN] > 0) {
				CreateArrow(mPosE[0], mPosE[1], out mArrowPoly, 8, 3);
			} else {
				var b = new PointF();
				InterpolationPost(ref b, 1 - (BODY_LEN - 1) / Post.Len, -5 * Post.Dsign * (int)Element.Para[ElmBJT.NPN]);
				CreateArrow(mPosE[1], b, out mArrowPoly, 8, 3);
			}
			SetTextPos();

			SetNodePos(Post.A, mPosC[2], mPosE[2]);
		}

		public override void Draw(CustomGraphics g) {
			/* draw collector */
			DrawLine(mPosC[2], mPosC[1]);
			DrawLine(mPosC[1], mPosC[0]);
			/* draw emitter */
			DrawLine(mPosE[2], mPosE[1]);
			DrawLine(mPosE[1], mPosE[0]);
			/* draw arrow */
			FillPolygon(mArrowPoly);
			/* draw base */
			DrawLine(Post.A, mTbase);
			/* draw base rectangle */
			FillPolygon(mRectPoly);

			/* draw dots */
			UpdateDotCount(-Element.I[ElmBJT.B], ref mCurCountB);
			UpdateDotCount(-Element.I[ElmBJT.C], ref mCurCountC);
			UpdateDotCount(-Element.I[ElmBJT.E], ref mCurCountE);
			DrawCurrent(mTbase, Post.A, mCurCountB);
			if (0 <= Element.Para[ElmBJT.NPN] * Element.I[ElmBJT.C]) {
				DrawCurrent(mPosE[1], mTbase, mCurCountB);
			} else {
				DrawCurrent(mPosC[1], mTbase, mCurCountB);
			}
			DrawCurrent(mPosE[1], mPosC[1], mCurCountC);

			if (ControlPanel.ChkShowName.Checked) {
				if (Post.Vertical) {
					DrawCenteredText(ReferenceName, mNamePos);
				} else {
					DrawCenteredText(ReferenceName, mNamePos, -Math.PI / 2);
				}
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = ((Element.Para[ElmBJT.NPN] < 0) ? "PNP" : "NPN") + "トランジスタ(" + "hfe：" + TextUtils.Unit(HFE) + ")";
			var vbc = Element.V[ElmBJT.B] - Element.V[ElmBJT.C];
			var vbe = Element.V[ElmBJT.B] - Element.V[ElmBJT.E];
			var vce = Element.V[ElmBJT.C] - Element.V[ElmBJT.E];
			if (vbc * Element.Para[ElmBJT.NPN] > 0.2) {
				arr[1] = "動作領域：" + (vbe * Element.Para[ElmBJT.NPN] > 0.2 ? "飽和" : "逆流");
			} else {
				arr[1] = "動作領域：" + (vbe * Element.Para[ElmBJT.NPN] > 0.2 ? "活性" : "遮断");
			}
			arr[2] = "Vce：" + TextUtils.Voltage(vce);
			arr[3] = "Vbe：" + TextUtils.Voltage(vbe);
			arr[4] = "Vbc：" + TextUtils.Voltage(vbc);
			arr[5] = "Ic：" + TextUtils.Current(Element.I[ElmBJT.C]);
			arr[6] = "Ib：" + TextUtils.Current(Element.I[ElmBJT.B]);
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("名前", ReferenceName);
			}
			if (r == 1) {
				return new ElementInfo("hfe", HFE);
			}
			if (r == 2) {
				return new ElementInfo("許容損失(W)", Element.Para[ElmBJT.MAX_POW]);
			}
			if (r == 3) {
				return new ElementInfo("エミッタ/コレクタ 入れ替え", (mFlags & FLAG_FLIP) != 0);
			}
			return null;
		}

		public override void SetElementValue(int r, int c, ElementInfo ei) {
			if (r == 0) {
				ReferenceName = ei.Text;
				SetTextPos();
			}
			if (r == 1) {
				SetHfe(ei.Value);
			}
			if (r == 2) {
				Element.Para[ElmBJT.MAX_POW] = ei.Value;
			}
			if (r == 3) {
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
