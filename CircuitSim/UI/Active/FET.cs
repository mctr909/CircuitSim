using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
	class FET : BaseUI {
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

		public FET(Point pos, bool isNch, bool mos) : base(pos) {
			if (mos) {
				Elm = new ElmFET(isNch, mos, 1.5, ElmFET.DefaultBeta);
			} else {
				Elm = new ElmFET(isNch, mos, isNch ? -1 : 1, 1.25);
			}
			mFlags = isNch ? 0 : FLAG_PNP;
			Post.NoDiagonal = true;
			ReferenceName = "Tr";
		}

		public FET(Point p1, Point p2, bool mos, int f, StringTokenizer st) : base(p1, p2, f) {
			var vt = st.nextTokenDouble(1.5);
			var beta = st.nextTokenDouble(ElmFET.DefaultBeta);
			Post.NoDiagonal = true;
			Elm = new ElmFET((f & FLAG_PNP) == 0, mos, vt, beta);
		}

		public override bool CanViewInScope { get { return true; } }

		public override DUMP_ID DumpId { get { return ((ElmFET)Elm).MOS ? DUMP_ID.MOSFET : DUMP_ID.JFET; } }

		protected override void dump(List<object> optionList) {
			var ce = (ElmFET)Elm;
			optionList.Add(ce.Vth);
			optionList.Add(ce.Beta);
		}

		public override void SetPoints() {
			base.SetPoints();
			var ce = (ElmFET)Elm;

			/* find the coordinates of the various points we need to draw the MOSFET. */
			var hsm = (HS / 8 + 1) * 8;
			int hs1 = hsm * Post.Dsign;
			var hs2 = HS * Post.Dsign;
			if ((mFlags & FLAG_FLIP) != 0) {
				hs1 = -hs1;
				hs2 = -hs2;
			}

			if (ce.MOS) {
				interpPostAB(ref mPosS[0], ref mPosD[0], 1, -hs1);
				interpPostAB(ref mPosS[3], ref mPosD[3], 1, -hs2);
				interpPostAB(ref mPosS[1], ref mPosD[1], 1 - 12 / Post.Len, -hs2);
				interpPostAB(ref mPosS[2], ref mPosD[2], 1 - 12 / Post.Len, -hs2 * 4 / 3);
			} else {
				interpPostAB(ref mPosS[0], ref mPosD[0], 1, -hs1);
				interpPostAB(ref mPosS[3], ref mPosD[3], 1, -hs2 * 3 / 4);
				interpPostAB(ref mPosS[1], ref mPosD[1], 1 - 16 / Post.Len, -hs2 * 3 / 4);
			}

			var gate = new PointF[2];
			interpPostAB(ref gate[0], ref gate[1], 1 - 16 / Post.Len, hs2 * 0.8);
			Utils.InterpPoint(gate[0], gate[1], out mGate, .5);

			const double ConnThick = 1.0;
			if (ce.MOS) {
				bool enhancement = ce.Vth > 0;
				var posS = mPosS[2];
				var posD = mPosD[2];
				if (enhancement) {
					var pD = new PointF[4];
					Utils.InterpPoint(posS, posD, out pD[0], 0.75, -ConnThick);
					Utils.InterpPoint(posS, posD, out pD[1], 0.75, ConnThick);
					Utils.InterpPoint(posS, posD, out pD[2], 1.0, ConnThick);
					Utils.InterpPoint(posS, posD, out pD[3], 1.0, -ConnThick);
					var pG = new PointF[4];
					Utils.InterpPoint(posS, posD, out pG[0], 3 / 8.0, -ConnThick);
					Utils.InterpPoint(posS, posD, out pG[1], 3 / 8.0, ConnThick);
					Utils.InterpPoint(posS, posD, out pG[2], 5 / 8.0, ConnThick);
					Utils.InterpPoint(posS, posD, out pG[3], 5 / 8.0, -ConnThick);
					var pS = new PointF[4];
					Utils.InterpPoint(posS, posD, out pS[0], 0.0, -ConnThick);
					Utils.InterpPoint(posS, posD, out pS[1], 0.0, ConnThick);
					Utils.InterpPoint(posS, posD, out pS[2], 0.25, ConnThick);
					Utils.InterpPoint(posS, posD, out pS[3], 0.25, -ConnThick);
					mPolyConn = new PointF[][] { pD, pG, pS };
				} else {
					mPolyConn = new PointF[1][] {
						new PointF[4]
					};
					Utils.InterpPoint(posS, posD, out mPolyConn[0][0], 0.0, -ConnThick);
					Utils.InterpPoint(posS, posD, out mPolyConn[0][1], 0.0, ConnThick);
					Utils.InterpPoint(posS, posD, out mPolyConn[0][2], 1.0, ConnThick);
					Utils.InterpPoint(posS, posD, out mPolyConn[0][3], 1.0, -ConnThick);
				}
				mPolyGate = new PointF[4];
				Utils.InterpPoint(gate[0], gate[1], out mPolyGate[0], 0.0, -ConnThick);
				Utils.InterpPoint(gate[0], gate[1], out mPolyGate[1], 0.0, ConnThick);
				Utils.InterpPoint(gate[0], gate[1], out mPolyGate[2], 1.0, ConnThick);
				Utils.InterpPoint(gate[0], gate[1], out mPolyGate[3], 1.0, -ConnThick);
				Utils.InterpPoint(mPosS[0], mPosD[0], out mPosB[0], 0.5);
				Utils.InterpPoint(mPosS[1], mPosD[1], out mPosB[1], 0.5);
				PointF a0, a1;
				if (ce.Nch == 1) {
					a0 = mPosB[0];
					a1 = mPosB[1];
				} else {
					a0 = mPosB[1];
					a1 = mPosB[0];
				}
				Utils.CreateArrow(a0, a1, out mArrowPoly, 8, 3);
			} else {
				const double GateLength = 0.25;
				mPolyConn = new PointF[0][];
				mPolyGate = new PointF[4];
				Utils.InterpPoint(gate[0], gate[1], out mPolyGate[0], -GateLength, -ConnThick);
				Utils.InterpPoint(gate[0], gate[1], out mPolyGate[1], -GateLength, ConnThick);
				Utils.InterpPoint(gate[0], gate[1], out mPolyGate[2], 1 + GateLength, ConnThick);
				Utils.InterpPoint(gate[0], gate[1], out mPolyGate[3], 1 + GateLength, -ConnThick);
				Utils.InterpPoint(mPosS[0], mPosD[0], out mPosB[0], 0.5);
				Utils.InterpPoint(mPosS[1], mPosD[1], out mPosB[1], 0.5);
				PointF a0, a1;
				double aLen;
				if (ce.Nch == 1) {
					a0 = Post.A;
					a1 = mPosB[0];
					aLen = 1.0 - 16 / Post.Len;
				} else {
					a0 = mPosB[0];
					a1 = Post.A;
					aLen = 28 / Post.Len;
				}
				PointF p;
				Utils.InterpPoint(a0, a1, out p, aLen);
				Utils.CreateArrow(a0, p, out mArrowPoly, 9, 3);
			}

			setTextPos();

			ce.SetNodePos(Post.A, mPosS[0], mPosD[0]);
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
				interpPost(ref mNamePos, 0.5, 10 * Post.Dsign);
			}
		}

		public override void Draw(CustomGraphics g) {
			var ce = (ElmFET)Elm;

			/* draw line connecting terminals to source/gate/drain */
			drawLine(Post.A, mGate);
			drawLine(mPosD[0], mPosD[3]);
			drawLine(mPosD[1], mPosD[3]);
			drawLine(mPosS[0], mPosS[3]);
			drawLine(mPosS[1], mPosS[3]);

			if (ce.MOS) {
				/* draw bulk connection */
				drawLine(ce.Nch == -1 ? mPosD[0] : mPosS[0], mPosB[0]);
				drawLine(mPosB[0], mPosB[1]);
			}

			/* draw source/drain */
			for (int i = 0; i != mPolyConn.Length; i++) {
				fillPolygon(mPolyConn[i]);
			}
			/* draw gate */
			fillPolygon(mPolyGate);
			/* draw arrow */
			fillPolygon(mArrowPoly);

			/* draw current */
			updateDotCount(-ce.Current, ref mCurCount);
			updateDotCount(ce.DiodeCurrent1, ref mCurcountBody1);
			updateDotCount(ce.DiodeCurrent2, ref mCurcountBody2);
			drawCurrent(mPosS[0], mPosB[0], mCurCount - mCurcountBody1);
			drawCurrent(mPosB[0], mPosD[0], mCurCount + mCurcountBody2);

			if (ControlPanel.ChkShowName.Checked) {
				if (Post.Vertical) {
					drawCenteredText(ReferenceName, mNamePos);
				} else {
					drawCenteredText(ReferenceName, mNamePos, -Math.PI / 2);
				}
			}
		}

		public override void GetInfo(string[] arr) {
			var ce = (ElmFET)Elm;
			arr[0] = ((ce.Nch == -1) ? "Pch MOSFET" : "Nch MOSFET") + "(閾値電圧：" + Utils.VoltageText(ce.Nch * ce.Vth) + ")";
			arr[1] = "動作領域：" + (
				(ce.Mode == 0) ? "遮断" :
				(ce.Mode == 1) ? "線形" : "飽和"
			);
			arr[2] = "Vgs：" + Utils.VoltageText(ce.Vg - (ce.Nch == -1 ? ce.Vd : ce.Vs));
			var vds = ce.Vd - ce.Vs;
			arr[3] = ((ce.Nch == 1) ? "Vds：" : "Vsd：") + Utils.VoltageText(vds);
			arr[4] = ((ce.Nch == 1) ? "Ids：" : "Isd：") + Utils.CurrentText(ce.Current);
			arr[5] = "R：" + Utils.UnitText(vds / ce.Current, CirSimForm.OHM_TEXT);
			arr[6] = "gm：" + Utils.UnitText(ce.Gm, "A/V");
			arr[7] = "Ib：" + Utils.UnitText(
				ce.BodyTerminal == 1 ? -ce.DiodeCurrent1 :
				ce.BodyTerminal == 2 ? ce.DiodeCurrent2 :
				-ce.Nch * (ce.DiodeCurrent1 + ce.DiodeCurrent2), "A");
		}

		public override ElementInfo GetElementInfo(int r, int c) {
			var ce = (ElmFET)Elm;
			if (c != 0) {
				return null;
			}
			if (r == 0) {
				return new ElementInfo("名前", ReferenceName);
			}
			if (r == 1) {
				return new ElementInfo("閾値電圧", ce.Nch * ce.Vth);
			}
			if (r == 2) {
				// 2Vds/(Ron*(Vgs-Vth)^2)
				return new ElementInfo("β", ce.Beta);
			}
			if (r == 3) {
				return new ElementInfo("ドレイン/ソース 入れ替え", (mFlags & FLAG_FLIP) != 0);
			}
			return null;
		}

		public override void SetElementValue(int n, int c, ElementInfo ei) {
			var ce = (ElmFET)Elm;
			if (n == 0) {
				ReferenceName = ei.Text;
				setTextPos();
			}
			if (n == 1) {
				ce.Vth = ce.Nch * ei.Value;
			}
			if (n == 2 && ei.Value > 0) {
				ce.Beta = ElmFET.LastBeta = ei.Value;
			}
			if (n == 3) {
				mFlags = ei.CheckBox.Checked ? (mFlags | FLAG_FLIP) : (mFlags & ~FLAG_FLIP);
				SetPoints();
			}
		}
	}
}
