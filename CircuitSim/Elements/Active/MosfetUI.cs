using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class MosfetUI : BaseUI {
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

        Point[] mSrc;
        Point[] mDrn;
        Point[] mGate;
        Point[] mBody;
        Point[] mArrowPoly;
        Point mPcircle;

        Point[] mPs1;
        Point[] mPs2;

        public MosfetUI(Point pos, bool pnpflag) : base(pos) {
            Elm = new MosfetElm(pnpflag);
            mFlags = pnpflag ? FLAG_PNP : 0;
            mFlags |= FLAG_BODY_DIODE;
            mNoDiagonal = true;
            ReferenceName = "Tr";
        }

        public MosfetUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new MosfetElm((f & FLAG_PNP) != 0, st);
            mNoDiagonal = true;
            try {
                ReferenceName = st.nextToken();
            } catch { }
            mGlobalFlags = mFlags & (FLAGS_GLOBAL);
        }

        public override bool CanViewInScope { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.MOSFET; } }

        protected override string dump() {
            var ce = (MosfetElm)Elm;
            return ce.Vt
                + " " + ce.Hfe
                + " " + ReferenceName;
        }

        bool DrawDigital { get { return (mFlags & FLAG_DIGITAL) != 0; } }

        bool ShowBulk { get { return (mFlags & (FLAG_DIGITAL | FLAG_HIDE_BULK)) == 0; } }

        /* post 0 = gate,
         * 1 = source for NPN,
         * 2 = drain for NPN,
         * 3 = body (if present)
         * for PNP, 1 is drain, 2 is source */
        public override Point GetPost(int n) {
            return (n == 0) ? mPost1 : (n == 1) ? mSrc[0] : (n == 2) ? mDrn[0] : mBody[0];
        }

        public override void SetPoints() {
            base.SetPoints();

            /* these two flags apply to all mosfets */
            mFlags &= ~FLAGS_GLOBAL;
            mFlags |= mGlobalFlags;

            /* find the coordinates of the various points we need to draw the MOSFET. */
            int hs2 = HS * mDsign;
            if ((mFlags & FLAG_FLIP) != 0) {
                hs2 = -hs2;
            }
            mSrc = new Point[3];
            mDrn = new Point[3];
            interpPointAB(ref mSrc[0], ref mDrn[0], 1, -hs2);
            interpPointAB(ref mSrc[1], ref mDrn[1], 1 - 18 / mLen, -hs2);
            interpPointAB(ref mSrc[2], ref mDrn[2], 1 - 18 / mLen, -hs2 * 4 / 3);

            mGate = new Point[3];
            interpPointAB(ref mGate[0], ref mGate[2], 1 - 24 / mLen, hs2 / 2);
            Utils.InterpPoint(mGate[0], mGate[2], ref mGate[1], .5);

            if (ShowBulk) {
                mBody = new Point[2];
                Utils.InterpPoint(mSrc[0], mDrn[0], ref mBody[0], .5);
                Utils.InterpPoint(mSrc[1], mDrn[1], ref mBody[1], .5);
            }

            var ce = (MosfetElm)Elm;

            if (!DrawDigital) {
                if (ce.Pnp == 1) {
                    if (ShowBulk) {
                        Utils.CreateArrow(mBody[0], mBody[1], out mArrowPoly, 10, 4);
                    } else {
                        Utils.CreateArrow(mSrc[1], mSrc[0], out mArrowPoly, 10, 4);
                    }
                } else {
                    if (ShowBulk) {
                        Utils.CreateArrow(mBody[1], mBody[0], out mArrowPoly, 10, 4);
                    } else {
                        Utils.CreateArrow(mDrn[0], mDrn[1], out mArrowPoly, 10, 4);
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
                Utils.InterpPoint(mSrc[1], mDrn[1], ref mPs1[i], i * SEG_F);
                Utils.InterpPoint(mSrc[1], mDrn[1], ref mPs2[i], (i + 1) * SEG_F);
            }

            setTextPos();
        }

        void setTextPos() {
            mNameV = mPost1.Y == mPost2.Y;
            var txtW = Context.GetTextSize(ReferenceName).Width;
            if (mNameV) {
                if (0 < mDsign) {
                    mNamePos = mPost2;
                } else {
                    mNamePos = new Point((int)(mPost2.X - txtW - 2), mPost2.Y);
                }
            } else if (mPost1.X == mPost2.X) {
                mNamePos = new Point(mPost2.X - (int)(txtW / 2), mPost2.Y + HS * mDsign * 2 / 3);
            } else {
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            /* pick up global flags changes */
            if ((mFlags & FLAGS_GLOBAL) != mGlobalFlags) {
                SetPoints();
            }

            setBbox(mPost1, mPost2, HS);

            /* draw source/drain terminals */
            drawLead(mSrc[0], mSrc[1]);
            drawLead(mDrn[0], mDrn[1]);

            var ce = (MosfetElm)Elm;

            /* draw line connecting source and drain */
            bool enhancement = ce.Vt > 0 && ShowBulk;
            for (int i = 0; i != SEGMENTS; i++) {
                if ((i == 1 || i == 4) && enhancement) {
                    continue;
                }
                drawLead(mPs1[i], mPs2[i]);
            }

            /* draw little extensions of that line */
            drawLead(mSrc[1], mSrc[2]);
            drawLead(mDrn[1], mDrn[2]);

            /* draw bulk connection */
            if (ShowBulk) {
                drawLead(ce.Pnp == -1 ? mDrn[0] : mSrc[0], mBody[0]);
                drawLead(mBody[0], mBody[1]);
            }

            /* draw arrow */
            if (!DrawDigital) {
                g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor, mArrowPoly);
            }

            /* draw gate */
            drawLead(mPost1, mGate[1]);
            drawLead(mGate[0], mGate[2]);
            if (DrawDigital && ce.Pnp == -1) {
                g.DrawCircle(mPcircle, mPcircler);
            }

            if ((mFlags & FLAG_SHOWVT) != 0) {
                string s = "" + (ce.Vt * ce.Pnp);
                drawCenteredLText(s, P2, false);
            }
            ce.CurCount = updateDotCount(-ce.Ids, ce.CurCount);
            drawDots(mSrc[0], mSrc[1], ce.CurCount);
            drawDots(mDrn[1], mDrn[0], ce.CurCount);
            drawDots(mSrc[1], mDrn[1], ce.CurCount);

            if (ShowBulk) {
                mCurcountBody1 = updateDotCount(ce.DiodeCurrent1, mCurcountBody1);
                mCurcountBody2 = updateDotCount(ce.DiodeCurrent2, mCurcountBody2);
                drawDots(mSrc[0], mBody[0], -mCurcountBody1);
                drawDots(mBody[0], mDrn[0], mCurcountBody2);
            }

            drawPosts();

            if (ControlPanel.ChkShowName.Checked) {
                if (mNameV) {
                    g.DrawCenteredVText(ReferenceName, mNamePos.X, mNamePos.Y);
                } else {
                    g.DrawLeftText(ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }

        public override void GetInfo(string[] arr) {
            getFetInfo(arr, "MOSFET");
        }

        void getFetInfo(string[] arr, string n) {
            var ce = (MosfetElm)Elm;
            arr[0] = ((ce.Pnp == -1) ? "p-" : "n-") + n;
            arr[0] += " (Vt=" + Utils.VoltageText(ce.Pnp * ce.Vt);
            arr[0] += ", \u03b2=" + ce.Hfe + ")";
            arr[1] = ((ce.Pnp == 1) ? "Ids = " : "Isd = ") + Utils.CurrentText(ce.Ids);
            arr[2] = "Vgs = " + Utils.VoltageText(ce.Volts[MosfetElm.V_G] - ce.Volts[ce.Pnp == -1 ? MosfetElm.V_D : MosfetElm.V_S]);
            arr[3] = ((ce.Pnp == 1) ? "Vds = " : "Vsd = ") + Utils.VoltageText(ce.Volts[MosfetElm.V_D] - ce.Volts[MosfetElm.V_S]);
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

        public override string GetScopeText(Scope.VAL v) {
            return ((((MosfetElm)Elm).Pnp == -1) ? "p-" : "n-") + "MOSFET";
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (MosfetElm)Elm;
            if (n == 0) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("閾値電圧", ce.Pnp * ce.Vt, .01, 5);
            }
            if (n == 2) {
                return new ElementInfo("hfe", ce.Hfe, .01, 5);
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "バルク表示",
                    Checked = ShowBulk
                };
                return ei;
            }
            if (n == 4) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "ドレイン/ソース 入れ替え",
                    Checked = (mFlags & FLAG_FLIP) != 0
                };
                return ei;
            }
            if (n == 5 && !ShowBulk) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "デジタル",
                    Checked = DrawDigital
                };
                return ei;
            }
            if (n == 5 && ShowBulk) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "還流ダイオード",
                    Checked = (mFlags & FLAG_BODY_DIODE) != 0
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (MosfetElm)Elm;
            if (n == 0) {
                ReferenceName = ei.Textf.Text;
                setTextPos();
            }
            if (n == 1) {
                ce.Vt = ce.Pnp * ei.Value;
            }
            if (n == 2 && ei.Value > 0) {
                ce.Hfe = MosfetElm.LastHfe = ei.Value;
            }
            if (n == 3) {
                mGlobalFlags = (!ei.CheckBox.Checked)
                    ? (mGlobalFlags | FLAG_HIDE_BULK) : (mGlobalFlags & ~(FLAG_HIDE_BULK | FLAG_DIGITAL));
                SetPoints();
                ei.NewDialog = true;
            }
            if (n == 4) {
                mFlags = ei.CheckBox.Checked
                    ? (mFlags | FLAG_FLIP) : (mFlags & ~FLAG_FLIP);
                SetPoints();
            }
            if (n == 5 && !ShowBulk) {
                mGlobalFlags = ei.CheckBox.Checked
                    ? (mGlobalFlags | FLAG_DIGITAL) : (mGlobalFlags & ~FLAG_DIGITAL);
                SetPoints();
            }
            if (n == 5 && ShowBulk) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_BODY_DIODE);
                ce.DoBodyDiode = 0 != (mFlags & FLAG_BODY_DIODE);
            }
        }
    }
}
