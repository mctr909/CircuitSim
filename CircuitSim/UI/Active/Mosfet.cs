﻿using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class Mosfet : BaseUI {
        const int FLAG_PNP = 1;
        const int FLAG_FLIP = 8;

        const int HS = 10;

        double mCurcountBody1;
        double mCurcountBody2;

        Point mGate;
        Point[] mPolyGate;
        Point[] mArrowPoly;

        Point[][] mPolyConn;
        Point[] mPosS = new Point[4];
        Point[] mPosD = new Point[4];
        Point[] mPosB = new Point[2];

        public Mosfet(Point pos, bool pnpflag) : base(pos) {
            Elm = new ElmMosfet(pnpflag);
            DumpInfo.Flags = pnpflag ? FLAG_PNP : 0;
            mNoDiagonal = true;
            DumpInfo.ReferenceName = "Tr";
        }

        public Mosfet(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var vt = ElmMosfet.DefaultThreshold;
            var hfe = ElmMosfet.DefaultHfe;
            try {
                vt = st.nextTokenDouble();
                hfe = st.nextTokenDouble();
            } catch { }
            mNoDiagonal = true;
            Elm = new ElmMosfet((f & FLAG_PNP) != 0, vt, hfe);
        }

        public override bool CanViewInScope { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.MOSFET; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmMosfet)Elm;
            optionList.Add(ce.Vt);
            optionList.Add(ce.Hfe);
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (ElmMosfet)Elm;

            /* find the coordinates of the various points we need to draw the MOSFET. */
            var hsm = (HS / 8 + 1) * 8;
            int hs1 = hsm * mDsign;
            var hs2 = HS * mDsign;
            if ((DumpInfo.Flags & FLAG_FLIP) != 0) {
                hs1 = -hs1;
                hs2 = -hs2;
            }
            interpPointAB(ref mPosS[0], ref mPosD[0], 1, -hs1);
            interpPointAB(ref mPosS[3], ref mPosD[3], 1, -hs2);
            interpPointAB(ref mPosS[1], ref mPosD[1], 1 - 12 / mLen, -hs2);
            interpPointAB(ref mPosS[2], ref mPosD[2], 1 - 12 / mLen, -hs2 * 4 / 3);

            var gate = new Point[2];
            interpPointAB(ref gate[0], ref gate[1], 1 - 16 / mLen, hs2 * 0.8);
            Utils.InterpPoint(gate[0], gate[1], ref mGate, .5);

            Utils.InterpPoint(mPosS[0], mPosD[0], ref mPosB[0], .5);
            Utils.InterpPoint(mPosS[1], mPosD[1], ref mPosB[1], .5);

            Point a0, a1;
            if (ce.Pnp == 1) {
                a0 = mPosB[0];
                a1 = mPosB[1];
            } else {
                a0 = mPosB[1];
                a1 = mPosB[0];
            }
            Utils.CreateArrow(a0.X, a0.Y, a1.X, a1.Y, out mArrowPoly, 8, 3);

            bool enhancement = ce.Vt > 0;
            var posS = mPosS[2];
            var posD = mPosD[2];
            if (enhancement) {
                var sdLen = Utils.Distance(posS, posD);
                var sdLenH = sdLen * 0.5;
                var pD = new Point[4];
                Utils.InterpPoint(posS, posD, ref pD[0], (sdLen - 6.0) / sdLen, -1);
                Utils.InterpPoint(posS, posD, ref pD[1], (sdLen - 6.0) / sdLen, 1);
                Utils.InterpPoint(posS, posD, ref pD[2], 1.0, 1);
                Utils.InterpPoint(posS, posD, ref pD[3], 1.0, -1);
                var pG = new Point[4];
                Utils.InterpPoint(posS, posD, ref pG[0], (sdLenH - 3.0) / sdLen, -1);
                Utils.InterpPoint(posS, posD, ref pG[1], (sdLenH - 3.0) / sdLen, 1);
                Utils.InterpPoint(posS, posD, ref pG[2], (sdLenH + 3.0) / sdLen, 1);
                Utils.InterpPoint(posS, posD, ref pG[3], (sdLenH + 3.0) / sdLen, -1);
                var pS = new Point[4];
                Utils.InterpPoint(posS, posD, ref pS[0], 0.0, -1);
                Utils.InterpPoint(posS, posD, ref pS[1], 0.0, 1);
                Utils.InterpPoint(posS, posD, ref pS[2], 6.0 / sdLen, 1);
                Utils.InterpPoint(posS, posD, ref pS[3], 6.0 / sdLen, -1);
                mPolyConn = new Point[][] { pD, pG, pS };
            } else {
                mPolyConn = new Point[1][];
                mPolyConn[0] = new Point[4];
                Utils.InterpPoint(posS, posD, ref mPolyConn[0][0], 0.0, -1);
                Utils.InterpPoint(posS, posD, ref mPolyConn[0][1], 0.0, 1);
                Utils.InterpPoint(posS, posD, ref mPolyConn[0][2], 1.0, 1);
                Utils.InterpPoint(posS, posD, ref mPolyConn[0][3], 1.0, -1);
            }

            mPolyGate = new Point[4];
            Utils.InterpPoint(gate[0], gate[1], ref mPolyGate[0], 0.0, -1);
            Utils.InterpPoint(gate[0], gate[1], ref mPolyGate[1], 0.0, 1);
            Utils.InterpPoint(gate[0], gate[1], ref mPolyGate[2], 1.0, 1);
            Utils.InterpPoint(gate[0], gate[1], ref mPolyGate[3], 1.0, -1);

            setTextPos();

            ce.Post[1] = mPosS[0];
            ce.Post[2] = mPosD[0];
        }

        void setTextPos() {
            if (mVertical) {
                mNamePos = new Point(Elm.Post[1].X, Elm.Post[1].Y + HS * mDsign * 2 / 3);
            } else if (mHorizontal) {
                if (0 < mDsign) {
                    mNamePos = new Point(Elm.Post[1].X - 1, Elm.Post[1].Y);
                } else {
                    mNamePos = new Point(Elm.Post[1].X - 16, Elm.Post[1].Y);
                }
            } else {
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            setBbox(HS);

            var ce = (ElmMosfet)Elm;
            var lineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;

            /* draw line connecting terminals to source/gate/drain */
            drawLead(mPosS[1], mPosS[3]);
            drawLead(mPosD[1], mPosD[3]);
            drawLead(mPosS[3], mPosS[0]);
            drawLead(mPosD[3], mPosD[0]);
            drawLead(Elm.Post[0], mGate);

            /* draw bulk connection */
            drawLead(ce.Pnp == -1 ? mPosD[0] : mPosS[0], mPosB[0]);
            drawLead(mPosB[0], mPosB[1]);

            /* draw source/drain */
            for (int i = 0; i != mPolyConn.Length; i++) {
                g.FillPolygon(lineColor, mPolyConn[i]);
            }

            /* draw arrow */
            g.FillPolygon(lineColor, mArrowPoly);

            /* draw gate */
            g.FillPolygon(lineColor, mPolyGate);

            /* draw current */
            updateDotCount(-ce.Current, ref CurCount);
            updateDotCount(ce.DiodeCurrent1, ref mCurcountBody1);
            updateDotCount(ce.DiodeCurrent2, ref mCurcountBody2);
            drawDots(mPosS[0], mPosS[1], CurCount);
            drawDots(mPosD[1], mPosD[0], CurCount);
            drawDots(mPosS[1], mPosD[1], CurCount);
            drawDots(mPosS[0], mPosB[0], -mCurcountBody1);
            drawDots(mPosB[0], mPosD[0], mCurcountBody2);

            drawPosts();

            if (ControlPanel.ChkShowName.Checked) {
                if (mVertical) {
                    g.DrawCenteredText(DumpInfo.ReferenceName, mNamePos.X, mNamePos.Y);
                } else {
                    g.DrawCenteredVText(DumpInfo.ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }

        public override void GetInfo(string[] arr) {
            getFetInfo(arr, "MOSFET");
        }

        void getFetInfo(string[] arr, string n) {
            var ce = (ElmMosfet)Elm;
            arr[0] = ((ce.Pnp == -1) ? "p-" : "n-") + n;
            arr[0] += " (Vt=" + Utils.VoltageText(ce.Pnp * ce.Vt);
            arr[0] += ", Hfe=" + ce.Hfe + ")";
            arr[1] = ((ce.Pnp == 1) ? "Ids = " : "Isd = ") + Utils.CurrentText(ce.Current);
            arr[2] = "Vgs = " + Utils.VoltageText(ce.Vg - (ce.Pnp == -1 ? ce.Vd : ce.Vs));
            arr[3] = ((ce.Pnp == 1) ? "Vds = " : "Vsd = ") + Utils.VoltageText(ce.Vd - ce.Vs);
            arr[4] = (ce.Mode == 0) ? "off" : (ce.Mode == 1) ? "線形" : "飽和";
            arr[5] = "gm = " + Utils.UnitText(ce.Gm, "A/V");
            arr[6] = "Ib = " + Utils.UnitText(
                ce.BodyTerminal == 1 ? -ce.DiodeCurrent1 :
                ce.BodyTerminal == 2 ? ce.DiodeCurrent2 :
                -ce.Pnp * (ce.DiodeCurrent1 + ce.DiodeCurrent2), "A");
        }

        public override string GetScopeText() {
            return (string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "MOSFET" : DumpInfo.ReferenceName) + " "
                + ((((ElmMosfet)Elm).Pnp == 1) ? "Vds(nCh.)" : "Vds(pCh.)");
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmMosfet)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("名前", DumpInfo.ReferenceName);
            }
            if (r == 1) {
                return new ElementInfo("閾値電圧", ce.Pnp * ce.Vt);
            }
            if (r == 2) {
                return new ElementInfo("hfe", ce.Hfe);
            }
            if (r == 3) {
                return new ElementInfo("ドレイン/ソース 入れ替え", (DumpInfo.Flags & FLAG_FLIP) != 0);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmMosfet)Elm;
            if (n == 0) {
                DumpInfo.ReferenceName = ei.Text;
                setTextPos();
            }
            if (n == 1) {
                ce.Vt = ce.Pnp * ei.Value;
            }
            if (n == 2 && ei.Value > 0) {
                ce.Hfe = ElmMosfet.LastHfe = ei.Value;
            }
            if (n == 3) {
                DumpInfo.Flags = ei.CheckBox.Checked
                    ? (DumpInfo.Flags | FLAG_FLIP) : (DumpInfo.Flags & ~FLAG_FLIP);
                SetPoints();
            }
        }
    }
}
