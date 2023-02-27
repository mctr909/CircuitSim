using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Active;

namespace Circuit.UI.Active {
    class Mosfet : BaseUI {
        const int FLAG_PNP = 1;
        const int FLAG_SHOWVT = 2;
        const int FLAG_DIGITAL = 4;
        const int FLAG_FLIP = 8;
        const int FLAG_HIDE_BULK = 16;
        const int FLAG_BODY_DIODE = 32;
        const int FLAGS_GLOBAL = (FLAG_HIDE_BULK | FLAG_DIGITAL);

        const int HS = 16;

        const int SEGMENTS = 6;
        const double SEG_F = 1.0 / SEGMENTS;

        static int mGlobalFlags;

        double mCurcountBody1;
        double mCurcountBody2;

        int mPcircler;

        Point[] mGate;
        Point[] mArrowPoly;
        Point mPcircle;

        Point[] mPs1;
        Point[] mPs2;

        public Mosfet(Point pos, bool pnpflag) : base(pos) {
            Elm = new ElmMosfet(pnpflag);
            DumpInfo.Flags = pnpflag ? FLAG_PNP : 0;
            DumpInfo.Flags |= FLAG_BODY_DIODE;
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
            mGlobalFlags = DumpInfo.Flags & (FLAGS_GLOBAL);
            Elm = new ElmMosfet((f & FLAG_PNP) != 0, vt, hfe);
            var ce = (ElmMosfet)Elm;
            ce.DoBodyDiode = 0 != (DumpInfo.Flags & FLAG_BODY_DIODE);
        }

        public override bool CanViewInScope { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.MOSFET; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmMosfet)Elm;
            optionList.Add(ce.Vt);
            optionList.Add(ce.Hfe);
        }

        bool DrawDigital { get { return (DumpInfo.Flags & FLAG_DIGITAL) != 0; } }

        bool ShowBulk { get { return (DumpInfo.Flags & (FLAG_DIGITAL | FLAG_HIDE_BULK)) == 0; } }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (ElmMosfet)Elm;

            /* these two flags apply to all mosfets */
            DumpInfo.Flags &= ~FLAGS_GLOBAL;
            DumpInfo.Flags |= mGlobalFlags;

            /* find the coordinates of the various points we need to draw the MOSFET. */
            int hs2 = HS * mDsign;
            if ((DumpInfo.Flags & FLAG_FLIP) != 0) {
                hs2 = -hs2;
            }
            ce.Src = new Point[3];
            ce.Drn = new Point[3];
            interpPointAB(ref ce.Src[0], ref ce.Drn[0], 1, -hs2);
            interpPointAB(ref ce.Src[1], ref ce.Drn[1], 1 - 18 / mLen, -hs2);
            interpPointAB(ref ce.Src[2], ref ce.Drn[2], 1 - 18 / mLen, -hs2 * 4 / 3);

            mGate = new Point[3];
            interpPointAB(ref mGate[0], ref mGate[2], 1 - 24 / mLen, hs2 / 2);
            Utils.InterpPoint(mGate[0], mGate[2], ref mGate[1], .5);

            if (ShowBulk) {
                ce.Body = new Point[2];
                Utils.InterpPoint(ce.Src[0], ce.Drn[0], ref ce.Body[0], .5);
                Utils.InterpPoint(ce.Src[1], ce.Drn[1], ref ce.Body[1], .5);
            }

            if (!DrawDigital) {
                var b0 = ce.Body[0];
                var b1 = ce.Body[1];
                if (ce.Pnp == 1) {
                    if (ShowBulk) {
                        Utils.CreateArrow(b0.X, b0.Y, b1.X, b1.Y, out mArrowPoly, 8, 3);
                    } else {
                        var s0 = ce.Src[0];
                        var s1 = ce.Src[1];
                        Utils.CreateArrow(s1.X, s1.Y, s0.X, s0.Y, out mArrowPoly, 8, 3);
                    }
                } else {
                    if (ShowBulk) {
                        Utils.CreateArrow(b1.X, b1.Y, b0.X, b0.Y, out mArrowPoly, 8, 3);
                    } else {
                        var d0 = ce.Drn[0];
                        var d1 = ce.Drn[1];
                        Utils.CreateArrow(d0.X, d0.Y, d1.X, d1.Y, out mArrowPoly, 8, 3);
                    }
                }
            } else if (ce.Pnp == -1) {
                interpPoint(ref mGate[1], 1 - 36 / mLen);
                int dist = (mDsign < 0) ? 32 : 31;
                interpPoint(ref mPcircle, 1 - dist / mLen);
                mPcircler = 3;
            }

            mPs1 = new Point[SEGMENTS];
            mPs2 = new Point[SEGMENTS];
            for (int i = 0; i != SEGMENTS; i++) {
                Utils.InterpPoint(ce.Src[1], ce.Drn[1], ref mPs1[i], i * SEG_F);
                Utils.InterpPoint(ce.Src[1], ce.Drn[1], ref mPs2[i], (i + 1) * SEG_F);
            }

            setTextPos();
        }

        void setTextPos() {
            if (mVertical) {
                mNamePos = new Point(Elm.Post2.X, Elm.Post2.Y + HS * mDsign * 2 / 3);
            } else if (mHorizontal) {
                if (0 < mDsign) {
                    mNamePos = new Point(Elm.Post2.X - 1, Elm.Post2.Y);
                } else {
                    mNamePos = new Point(Elm.Post2.X - 16, Elm.Post2.Y);
                }
            } else {
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            /* pick up global flags changes */
            if ((DumpInfo.Flags & FLAGS_GLOBAL) != mGlobalFlags) {
                SetPoints();
            }

            setBbox(HS);

            var ce = (ElmMosfet)Elm;

            /* draw source/drain terminals */
            drawLead(ce.Src[0], ce.Src[1]);
            drawLead(ce.Drn[0], ce.Drn[1]);

            /* draw little extensions of that line */
            drawLead(ce.Src[1], ce.Src[2]);
            drawLead(ce.Drn[1], ce.Drn[2]);

            /* draw line connecting source and drain */
            bool enhancement = ce.Vt > 0 && ShowBulk;
            for (int i = 0; i != SEGMENTS; i++) {
                if ((i == 1 || i == 4) && enhancement) {
                    continue;
                }
                drawLead(mPs1[i], mPs2[i]);
            }

            /* draw bulk connection */
            if (ShowBulk) {
                drawLead(ce.Pnp == -1 ? ce.Drn[0] : ce.Src[0], ce.Body[0]);
                drawLead(ce.Body[0], ce.Body[1]);
            }

            /* draw arrow */
            if (!DrawDigital) {
                g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor, mArrowPoly);
            }

            /* draw gate */
            drawLead(Elm.Post1, mGate[1]);
            drawLead(mGate[0], mGate[2]);
            if (DrawDigital && ce.Pnp == -1) {
                g.DrawCircle(mPcircle, mPcircler);
            }

            if ((DumpInfo.Flags & FLAG_SHOWVT) != 0) {
                string s = "" + (ce.Vt * ce.Pnp);
                drawCenteredLText(s, DumpInfo.P2X, DumpInfo.P2Y, false);
            }
            CurCount = updateDotCount(-ce.Current, CurCount);
            drawDots(ce.Src[0], ce.Src[1], CurCount);
            drawDots(ce.Drn[1], ce.Drn[0], CurCount);
            drawDots(ce.Src[1], ce.Drn[1], CurCount);

            if (ShowBulk) {
                mCurcountBody1 = updateDotCount(ce.DiodeCurrent1, mCurcountBody1);
                mCurcountBody2 = updateDotCount(ce.DiodeCurrent2, mCurcountBody2);
                drawDots(ce.Src[0], ce.Body[0], -mCurcountBody1);
                drawDots(ce.Body[0], ce.Drn[0], mCurcountBody2);
            }

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
            arr[0] += ", \u03b2=" + ce.Hfe + ")";
            arr[1] = ((ce.Pnp == 1) ? "Ids = " : "Isd = ") + Utils.CurrentText(ce.Current);
            arr[2] = "Vgs = " + Utils.VoltageText(ce.Vg - (ce.Pnp == -1 ? ce.Vd : ce.Vs));
            arr[3] = ((ce.Pnp == 1) ? "Vds = " : "Vsd = ") + Utils.VoltageText(ce.Vd - ce.Vs);
            arr[4] = (ce.Mode == 0) ? "off" : (ce.Mode == 1) ? "linear" : "saturation";
            arr[5] = "gm = " + Utils.UnitText(ce.Gm, "A/V");
            arr[6] = "P = " + Utils.UnitText(ce.Power, "W");
            if (ShowBulk) {
                arr[7] = "Ib = " + Utils.UnitText(
                    ce.BodyTerminal == 1 ? -ce.DiodeCurrent1 :
                    ce.BodyTerminal == 2 ? ce.DiodeCurrent2 :
                    -ce.Pnp * (ce.DiodeCurrent1 + ce.DiodeCurrent2), "A");
            }
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
                return new ElementInfo("バルク表示", ShowBulk);
            }
            if (r == 4) {
                return new ElementInfo("ドレイン/ソース 入れ替え", (DumpInfo.Flags & FLAG_FLIP) != 0);
            }
            if (r == 5 && !ShowBulk) {
                return new ElementInfo("デジタル", DrawDigital);
            }
            if (r == 5 && ShowBulk) {
                return new ElementInfo("還流ダイオード", (DumpInfo.Flags & FLAG_BODY_DIODE) != 0);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmMosfet)Elm;
            if (n == 0) {
                DumpInfo.ReferenceName = ei.Textf.Text;
                setTextPos();
            }
            if (n == 1) {
                ce.Vt = ce.Pnp * ei.Value;
            }
            if (n == 2 && ei.Value > 0) {
                ce.Hfe = ElmMosfet.LastHfe = ei.Value;
            }
            if (n == 3) {
                mGlobalFlags = (!ei.CheckBox.Checked)
                    ? (mGlobalFlags | FLAG_HIDE_BULK) : (mGlobalFlags & ~(FLAG_HIDE_BULK | FLAG_DIGITAL));
                SetPoints();
                ei.NewDialog = true;
            }
            if (n == 4) {
                DumpInfo.Flags = ei.CheckBox.Checked
                    ? (DumpInfo.Flags | FLAG_FLIP) : (DumpInfo.Flags & ~FLAG_FLIP);
                SetPoints();
            }
            if (n == 5 && !ShowBulk) {
                mGlobalFlags = ei.CheckBox.Checked
                    ? (mGlobalFlags | FLAG_DIGITAL) : (mGlobalFlags & ~FLAG_DIGITAL);
                SetPoints();
            }
            if (n == 5 && ShowBulk) {
                DumpInfo.Flags = ei.ChangeFlag(DumpInfo.Flags, FLAG_BODY_DIODE);
                ce.DoBodyDiode = 0 != (DumpInfo.Flags & FLAG_BODY_DIODE);
            }
        }
    }
}
