using Circuit.Elements.Active;

namespace Circuit.Symbol.Active {
	class FET : BaseSymbol {
		const int FLAG_PNP = 1;
		const int FLAG_FLIP = 8;

		const int HS = 10;

		ElmFET mElm;
		double mCurcountBody1;
		double mCurcountBody2;

		PointF mGate;
		PointF[] mPolyGate;
		PointF[] mArrowPoly;

		PointF[][] mPolyConn;
		PointF[] mPosS = new PointF[4];
		PointF[] mPosD = new PointF[4];
		PointF[] mPosB = new PointF[2];

		public override BaseElement Element { get { return mElm; } }

		public FET(Point pos, bool isNch, bool mos) : base(pos) {
			if (mos) {
				mElm = new ElmFET(isNch, mos, 1.5, ElmFET.DefaultBeta);
			} else {
				mElm = new ElmJFET(isNch, isNch ? -1 : 1, 0.00125);
			}
			mFlags = isNch ? 0 : FLAG_PNP;
			Post.NoDiagonal = true;
			ReferenceName = "Tr";
		}

		public FET(Point p1, Point p2, bool mos, int f, StringTokenizer st) : base(p1, p2, f) {
			var vt = st.nextTokenDouble(1.5);
			var beta = st.nextTokenDouble(ElmFET.DefaultBeta);
			Post.NoDiagonal = true;
			if (mos) {
				mElm = new ElmFET((f & FLAG_PNP) == 0, mos, vt, beta);
			} else {
				mElm = new ElmJFET((f & FLAG_PNP) == 0, vt, beta);
			}
		}

		public override bool CanViewInScope { get { return true; } }

		public override DUMP_ID DumpId { get { return mElm.MOS ? DUMP_ID.MOSFET : DUMP_ID.JFET; } }

		protected override void dump(List<object> optionList) {
			optionList.Add(mElm.Vth);
			optionList.Add(mElm.Beta);
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

			if (mElm.MOS) {
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
			if (mElm.MOS) {
				bool enhancement = mElm.Vth > 0;
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
				if (mElm.Nch == 1) {
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
				if (mElm.Nch == 1) {
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

			mElm.SetNodePos(Post.A, mPosS[0], mPosD[0]);
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

			if (mElm.MOS) {
				/* draw bulk connection */
				DrawLine(mElm.Nch == -1 ? mPosD[0] : mPosS[0], mPosB[0]);
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
			UpdateDotCount(-mElm.Current, ref mCurCount);
			UpdateDotCount(mElm.DiodeCurrent1, ref mCurcountBody1);
			UpdateDotCount(mElm.DiodeCurrent2, ref mCurcountBody2);
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
			arr[0] = ((mElm.Nch == -1) ? "Pch MOSFET" : "Nch MOSFET") + "(閾値電圧：" + TextUtils.Voltage(mElm.Nch * mElm.Vth) + ")";
			arr[1] = "動作領域：" + (
				(mElm.Mode == 0) ? "遮断" :
				(mElm.Mode == 1) ? "線形" : "飽和"
			);
			arr[2] = "Vgs：" + TextUtils.Voltage(mElm.Vg - (mElm.Nch == -1 ? mElm.Vd : mElm.Vs));
			var vds = mElm.Vd - mElm.Vs;
			arr[3] = ((mElm.Nch == 1) ? "Vds：" : "Vsd：") + TextUtils.Voltage(vds);
			arr[4] = ((mElm.Nch == 1) ? "Ids：" : "Isd：") + TextUtils.Current(mElm.Current);
			arr[5] = "R：" + TextUtils.Unit(vds / mElm.Current, "Ω");
			arr[6] = "gm：" + TextUtils.Unit(mElm.Gm, "A/V");
			arr[7] = "Ib：" + TextUtils.Unit(
				mElm.BodyTerminal == 1 ? -mElm.DiodeCurrent1 :
				mElm.BodyTerminal == 2 ? mElm.DiodeCurrent2 :
				-mElm.Nch * (mElm.DiodeCurrent1 + mElm.DiodeCurrent2), "A");
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("名前", ReferenceName);
			}
			if (r == 1) {
				return new ElementInfo("閾値電圧", mElm.Nch * mElm.Vth);
			}
			if (r == 2) {
				// 2Vds/(Ron*(Vgs-Vth)^2)
				return new ElementInfo("β", mElm.Beta);
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
				mElm.Vth = mElm.Nch * ei.Value;
			}
			if (n == 2 && ei.Value > 0) {
				mElm.Beta = ei.Value;
			}
			if (n == 3) {
				mFlags = ei.CheckBox.Checked ? (mFlags | FLAG_FLIP) : (mFlags & ~FLAG_FLIP);
				SetPoints();
			}
		}
	}
}
