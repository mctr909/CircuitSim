using Circuit.Elements.Active;
using Circuit.Elements;
using MainForm.Forms;

namespace Circuit.Symbol.Active {
	class FET : BaseSymbol {
		const int FLAG_PNP = 1;
		const int FLAG_FLIP = 8;

		const int HS = 10;

		double mCurcountBody1;
		double mCurcountBody2;

		PointF mGate;
		PointF[] mPolyGate;
		PointF[] mArrowPoly;

		PointF[][] mPolyConn;
		PointF[] mPosS = new PointF[4];
		PointF[] mPosD = new PointF[4];
		PointF[] mPosB = new PointF[2];

		public override bool HasConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

		public FET(Point pos, bool isNch, bool mos) : base(pos) {
			if (mos) {
				Element.Para[ElmFET.N_CH] = isNch ? 1 : -1;
				Element.State[ElmFET.MOS] = 1;
				Element.Para[ElmFET.V_TH] = 1.5;
				Element.Para[ElmFET.BETA] = 1;
			} else {
				Element.Para[ElmFET.N_CH] = isNch ? 1 : -1;
				Element.State[ElmFET.MOS] = 0;
				Element.Para[ElmFET.V_TH] = isNch ? -1 : 1;
				Element.Para[ElmFET.BETA] = 0.00125;
			}
			mFlags = isNch ? 0 : FLAG_PNP;
			Post.NoDiagonal = true;
			ReferenceName = "Tr";
		}

		public FET(Point p1, Point p2, bool mos, int f, StringTokenizer st) : base(p1, p2, f) {
			Post.NoDiagonal = true;
			if (mos) {
				var vt = st.nextTokenDouble(1.5);
				var beta = st.nextTokenDouble(1);
				Element.Para[ElmFET.N_CH] = (f & FLAG_PNP) == 0 ? 1 : -1;
				Element.State[ElmFET.MOS] = 1;
				Element.Para[ElmFET.V_TH] = vt;
				Element.Para[ElmFET.BETA] = beta;
			} else {
				var vt = st.nextTokenDouble((f & FLAG_PNP) == 0 ? -1 : 1);
				var beta = st.nextTokenDouble(0.00125);
				Element.Para[ElmFET.N_CH] = (f & FLAG_PNP) == 0 ? 1 : -1;
				Element.State[ElmFET.MOS] = 0;
				Element.Para[ElmFET.V_TH] = vt;
				Element.Para[ElmFET.BETA] = beta;
			}
		}

		protected override BaseElement Create() {
			return new ElmFET();
		}

		public override bool CanViewInScope { get { return true; } }

		public override DUMP_ID DumpId { get { return Element.State[ElmFET.MOS] == 0 ? DUMP_ID.JFET : DUMP_ID.MOSFET; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(Element.Para[ElmFET.V_TH]);
			optionList.Add(Element.Para[ElmFET.BETA]);
		}

		public override void Reset() {
			Element.V[ElmFET.G] = 0;
			Element.V[ElmFET.S] = 0;
			Element.V[ElmFET.D] = 0;
			Element.V[ElmFET.G_LAST] = 0;
			Element.V[ElmFET.S_LAST] = 0;
			Element.V[ElmFET.D_LAST] = 0;
			Element.V[ElmFET.VD_D1] = 0;
			Element.V[ElmFET.VD_D2] = 0;
			Element.I[ElmFET.CUR_D1] = 0;
			Element.I[ElmFET.CUR_D2] = 0;
		}

		public override void Stamp() {
			StampNonLinear(Element.Nodes[ElmFET.S]);
			StampNonLinear(Element.Nodes[ElmFET.D]);
			var elm = (ElmFET)Element;
			elm.BODY = (Element.Para[ElmFET.N_CH] > 0) ? ElmFET.S : ElmFET.D;
			if (Element.State[ElmFET.MOS] != 0) {
				if (Element.Para[ElmFET.N_CH] > 0) {
					elm.D1_A = elm.BODY;
					elm.D1_B = ElmFET.S;
					elm.D2_A = elm.BODY;
					elm.D2_B = ElmFET.D;
				} else {
					elm.D1_A = ElmFET.S;
					elm.D1_B = elm.BODY;
					elm.D2_A = ElmFET.D;
					elm.D2_B = elm.BODY;
				}
				StampNonLinear(Element.Nodes[elm.D1_A]);
				StampNonLinear(Element.Nodes[elm.D1_B]);
				StampNonLinear(Element.Nodes[elm.D2_A]);
				StampNonLinear(Element.Nodes[elm.D2_B]);
			}
		}

		public override void SetPoints() {
			base.SetPoints();

			/* find the coordinates of the various points we need to draw the MOSFET. */
			var hsm = (HS / 8 + 1) * 8;
			int hs1 = hsm * Post.Dsign;
			var hs2 = HS * Post.Dsign;
			if ((mFlags & FLAG_FLIP) != 0) {
				hs1 = -hs1;
				hs2 = -hs2;
			}

			if (Element.State[ElmFET.MOS] != 0) {
				InterpolationPostAB(ref mPosS[0], ref mPosD[0], 1, -hs1);
				InterpolationPostAB(ref mPosS[3], ref mPosD[3], 1, -hs2);
				InterpolationPostAB(ref mPosS[1], ref mPosD[1], 1 - 12 / Post.Len, -hs2);
				InterpolationPostAB(ref mPosS[2], ref mPosD[2], 1 - 12 / Post.Len, -hs2 * 4 / 3);
			} else {
				InterpolationPostAB(ref mPosS[0], ref mPosD[0], 1, -hs1);
				InterpolationPostAB(ref mPosS[3], ref mPosD[3], 1, -hs2 * 3 / 4);
				InterpolationPostAB(ref mPosS[1], ref mPosD[1], 1 - 16 / Post.Len, -hs2 * 3 / 4);
			}

			var gate = new PointF[2];
			InterpolationPostAB(ref gate[0], ref gate[1], 1 - 16 / Post.Len, hs2 * 0.8);
			InterpolationPoint(gate[0], gate[1], out mGate, .5);

			const double ConnThick = 1.0;
			if (Element.State[ElmFET.MOS] != 0) {
				bool enhancement = Element.Para[ElmFET.V_TH] > 0;
				var posS = mPosS[2];
				var posD = mPosD[2];
				if (enhancement) {
					var pD = new PointF[4];
					InterpolationPoint(posS, posD, out pD[0], 0.75, -ConnThick);
					InterpolationPoint(posS, posD, out pD[1], 0.75, ConnThick);
					InterpolationPoint(posS, posD, out pD[2], 1.0, ConnThick);
					InterpolationPoint(posS, posD, out pD[3], 1.0, -ConnThick);
					var pG = new PointF[4];
					InterpolationPoint(posS, posD, out pG[0], 3 / 8.0, -ConnThick);
					InterpolationPoint(posS, posD, out pG[1], 3 / 8.0, ConnThick);
					InterpolationPoint(posS, posD, out pG[2], 5 / 8.0, ConnThick);
					InterpolationPoint(posS, posD, out pG[3], 5 / 8.0, -ConnThick);
					var pS = new PointF[4];
					InterpolationPoint(posS, posD, out pS[0], 0.0, -ConnThick);
					InterpolationPoint(posS, posD, out pS[1], 0.0, ConnThick);
					InterpolationPoint(posS, posD, out pS[2], 0.25, ConnThick);
					InterpolationPoint(posS, posD, out pS[3], 0.25, -ConnThick);
					mPolyConn = new PointF[][] { pD, pG, pS };
				} else {
					mPolyConn = new PointF[1][] {
						new PointF[4]
					};
					InterpolationPoint(posS, posD, out mPolyConn[0][0], 0.0, -ConnThick);
					InterpolationPoint(posS, posD, out mPolyConn[0][1], 0.0, ConnThick);
					InterpolationPoint(posS, posD, out mPolyConn[0][2], 1.0, ConnThick);
					InterpolationPoint(posS, posD, out mPolyConn[0][3], 1.0, -ConnThick);
				}
				mPolyGate = new PointF[4];
				InterpolationPoint(gate[0], gate[1], out mPolyGate[0], 0.0, -ConnThick);
				InterpolationPoint(gate[0], gate[1], out mPolyGate[1], 0.0, ConnThick);
				InterpolationPoint(gate[0], gate[1], out mPolyGate[2], 1.0, ConnThick);
				InterpolationPoint(gate[0], gate[1], out mPolyGate[3], 1.0, -ConnThick);
				InterpolationPoint(mPosS[0], mPosD[0], out mPosB[0], 0.5);
				InterpolationPoint(mPosS[1], mPosD[1], out mPosB[1], 0.5);
				PointF a0, a1;
				if (Element.Para[ElmFET.N_CH] > 0) {
					a0 = mPosB[0];
					a1 = mPosB[1];
				} else {
					a0 = mPosB[1];
					a1 = mPosB[0];
				}
				CreateArrow(a0, a1, out mArrowPoly, 8, 3);
			} else {
				const double GateLength = 0.25;
				mPolyConn = new PointF[0][];
				mPolyGate = new PointF[4];
				InterpolationPoint(gate[0], gate[1], out mPolyGate[0], -GateLength, -ConnThick);
				InterpolationPoint(gate[0], gate[1], out mPolyGate[1], -GateLength, ConnThick);
				InterpolationPoint(gate[0], gate[1], out mPolyGate[2], 1 + GateLength, ConnThick);
				InterpolationPoint(gate[0], gate[1], out mPolyGate[3], 1 + GateLength, -ConnThick);
				InterpolationPoint(mPosS[0], mPosD[0], out mPosB[0], 0.5);
				InterpolationPoint(mPosS[1], mPosD[1], out mPosB[1], 0.5);
				PointF a0, a1;
				double aLen;
				if (Element.Para[ElmFET.N_CH] > 0) {
					a0 = Post.A;
					a1 = mPosB[0];
					aLen = 1.0 - 16 / Post.Len;
				} else {
					a0 = mPosB[0];
					a1 = Post.A;
					aLen = 28 / Post.Len;
				}
				InterpolationPoint(a0, a1, out PointF p, aLen);
				CreateArrow(a0, p, out mArrowPoly, 9, 3);
			}

			setTextPos();

			SetNodePos(Post.A, mPosS[0], mPosD[0]);
		}

		void setTextPos() {
			if (Post.Horizontal) {
				if (0 < Post.Dsign) {
					mNamePos = new Point(Post.B.X + 10, Post.B.Y);
				} else {
					mNamePos = new Point(Post.B.X - 6, Post.B.Y);
				}
			} else if (Post.Vertical) {
				if (0 < Post.Dsign) {
					mNamePos = new Point(Post.B.X, Post.B.Y + 15 * 2 / 3);
				} else {
					mNamePos = new Point(Post.B.X, Post.B.Y - 13 * 2 / 3);
				}
			} else {
				InterpolationPost(ref mNamePos, 0.5, 10 * Post.Dsign);
			}
		}

		public override void Draw(CustomGraphics g) {
			/* draw line connecting terminals to source/gate/drain */
			DrawLine(Post.A, mGate);
			DrawLine(mPosD[0], mPosD[3]);
			DrawLine(mPosD[1], mPosD[3]);
			DrawLine(mPosS[0], mPosS[3]);
			DrawLine(mPosS[1], mPosS[3]);

			if (Element.State[ElmFET.MOS] != 0) {
				/* draw bulk connection */
				DrawLine(Element.Para[ElmFET.N_CH] < 0 ? mPosD[0] : mPosS[0], mPosB[0]);
				DrawLine(mPosB[0], mPosB[1]);
			}

			/* draw source/drain */
			for (int i = 0; i != mPolyConn.Length; i++) {
				FillPolygon(mPolyConn[i]);
			}
			/* draw gate */
			FillPolygon(mPolyGate);
			/* draw arrow */
			FillPolygon(mArrowPoly);

			/* draw current */
			UpdateDotCount(-Element.I[0], ref mCurCount);
			UpdateDotCount(Element.I[ElmFET.CUR_D1], ref mCurcountBody1);
			UpdateDotCount(Element.I[ElmFET.CUR_D2], ref mCurcountBody2);
			DrawCurrent(mPosS[0], mPosB[0], mCurCount - mCurcountBody1);
			DrawCurrent(mPosB[0], mPosD[0], mCurCount + mCurcountBody2);

			if (ControlPanel.ChkShowName.Checked) {
				if (Post.Vertical) {
					DrawCenteredText(ReferenceName, mNamePos);
				} else {
					DrawCenteredText(ReferenceName, mNamePos, -Math.PI / 2);
				}
			}
		}

		public override void GetInfo(string[] arr) {
			arr[0] = (Element.Para[ElmFET.N_CH] < 0 ? "Pch MOSFET" : "Nch MOSFET")
				+ "(閾値電圧：" + TextUtils.Voltage(Element.Para[ElmFET.N_CH] * Element.Para[ElmFET.V_TH]) + ")";
			arr[1] = "Vgs：" + TextUtils.Voltage(Element.V[ElmFET.G] - (Element.Para[ElmFET.N_CH] < 0 ? Element.V[ElmFET.D] : Element.V[ElmFET.S]));
			var vds = Element.V[ElmFET.D] - Element.V[ElmFET.S];
			arr[2] = (Element.Para[ElmFET.N_CH] > 0 ? "Vds：" : "Vsd：") + TextUtils.Voltage(vds);
			arr[3] = (Element.Para[ElmFET.N_CH] > 0 ? "Ids：" : "Isd：") + TextUtils.Current(Element.I[0]);
			arr[4] = "Rds：" + TextUtils.Unit(vds / Element.I[0], "Ω");
			arr[5] = "gm：" + TextUtils.Unit(Element.Para[ElmFET.GM], "A/V");
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("名前", ReferenceName);
			}
			if (r == 1) {
				return new ElementInfo("閾値電圧", Element.Para[ElmFET.N_CH] * Element.Para[ElmFET.V_TH]);
			}
			if (r == 2) {
				// 2Vds/(Ron*(Vgs-Vth)^2)
				return new ElementInfo("β", Element.Para[ElmFET.BETA]);
			}
			if (r == 3) {
				return new ElementInfo("ドレイン/ソース 入れ替え", (mFlags & FLAG_FLIP) != 0);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			if (n == 0) {
				ReferenceName = ei.Text;
				setTextPos();
			}
			if (n == 1) {
				Element.Para[ElmFET.V_TH] = Element.Para[ElmFET.N_CH] * ei.Value;
			}
			if (n == 2 && ei.Value > 0) {
				Element.Para[ElmFET.BETA] = ei.Value;
			}
			if (n == 3) {
				mFlags = ei.CheckBox.Checked ? (mFlags | FLAG_FLIP) : (mFlags & ~FLAG_FLIP);
				SetPoints();
			}
		}
	}
}
