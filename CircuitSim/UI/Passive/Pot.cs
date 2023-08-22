using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Passive;

namespace Circuit.UI.Passive {
    class Pot : BaseUI {
        const int FLAG_SHOW_VALUES = 1;

        const int HS = 5;
        const int BODY_LEN = 24;
        const int SEGMENTS = 12;
        const double SEG_F = 1.0 / SEGMENTS;

        Point mCorner2;
        Point mArrowPoint;
        Point mMidPoint;
        Point mArrow1;
        Point mArrow2;
        Point[] mPs1;
        Point[] mPs2;
        Point[] mRect1;
        Point[] mRect2;
        Point[] mRect3;
        Point[] mRect4;

        TrackBar mSlider;
        Label mLabel;

        public Pot(Point pos) : base(pos) {
            Elm = new ElmPot();
            Elm.AllocNodes();
            DumpInfo.Flags = FLAG_SHOW_VALUES;
            DumpInfo.ReferenceName = "VR";
            createSlider();
        }

        public Pot(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            var elm = new ElmPot();
            Elm = elm;
            try {
                elm.MaxResistance = st.nextTokenDouble();
                elm.Position = st.nextTokenDouble();
            } catch (Exception ex) {
                throw new Exception("Pot load error:{0}", ex);
            }
            elm.AllocNodes();
            createSlider();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.POT; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmPot)Elm;
            optionList.Add(ce.MaxResistance);
            optionList.Add(ce.Position);
        }

        public override void SetMouseElm(bool v) {
            base.SetMouseElm(v);
        }

        public override void Delete() {
            ControlPanel.RemoveSlider(mLabel);
            ControlPanel.RemoveSlider(mSlider);
            base.Delete();
        }

        public override void SetPoints() {
            base.SetPoints();
            int offset = 0;
            int myLen = 0;
            mVertical = Math.Abs(mDiff.X) <= Math.Abs(mDiff.Y);
            if (mVertical) {
                /* vertical */
                myLen = 2 * CirSimForm.GRID_SIZE * Math.Sign(mDiff.Y)
                    * ((Math.Abs(mDiff.Y) + 2 * CirSimForm.GRID_SIZE - 1) / (2 * CirSimForm.GRID_SIZE));
                if (mDiff.Y != 0) {
                    Elm.Post[1].Y = Elm.Post[0].Y + myLen;
                    offset = (0 < mDiff.Y) ? mDiff.X : -mDiff.X;
                    Elm.Post[1].X = Elm.Post[0].X;
                }
            } else {
                /* horizontal */
                myLen = 2 * CirSimForm.GRID_SIZE * Math.Sign(mDiff.X)
                    * ((Math.Abs(mDiff.X) + 2 * CirSimForm.GRID_SIZE - 1) / (2 * CirSimForm.GRID_SIZE));
                Elm.Post[1].X = Elm.Post[0].X + myLen;
                offset = (mDiff.X < 0) ? mDiff.Y : -mDiff.Y;
                Elm.Post[1].Y = Elm.Post[0].Y;
            }
            if (offset < CirSimForm.GRID_SIZE) {
                offset = CirSimForm.GRID_SIZE;
            }
            mLen = Utils.Distance(Elm.Post[0], Elm.Post[1]);

            calcLeads(BODY_LEN);

            /* set slider */
            var ce = (ElmPot)Elm;
            ce.Position = mSlider.Value * 0.0099 + 0.0001;
            int soff = (int)((ce.Position - 0.5) * BODY_LEN);
            interpPost(ref ce.Post[2], 0.5, offset);
            interpPost(ref mCorner2, soff / mLen + 0.5, offset);
            interpPost(ref mArrowPoint, soff / mLen + 0.5, 8 * Math.Sign(offset));
            interpPost(ref mMidPoint, soff / mLen + 0.5);
            double clen = Math.Abs(offset) - 8;
            Utils.InterpPoint(mCorner2, mArrowPoint, ref mArrow1, ref mArrow2, (clen - 8) / clen, 4);

            setNamePos();

            setPoly();
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmPot)Elm;
            double vl = ce.Volts[ElmPot.V_L];
            double vr = ce.Volts[ElmPot.V_R];
            double vs = ce.Volts[ElmPot.V_S];
            setBbox(HS);
            draw2Leads();

            int divide = (int)(SEGMENTS * ce.Position);

            if (ControlPanel.ChkUseAnsiSymbols.Checked) {
                /* draw zigzag */
                for (int i = 0; i != SEGMENTS; i++) {
                    double v = vl + (vs - vl) * i / divide;
                    if (i >= divide) {
                        v = vs + (vr - vs) * (i - divide) / (SEGMENTS - divide);
                    }
                    drawLine(mPs1[i], mPs2[i]);
                }
            } else {
                /* draw rectangle */
                drawLine(mRect1[0], mRect2[0]);
                for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
                    double v = vl + (vs - vl) * i / divide;
                    if (i >= divide) {
                        v = vs + (vr - vs) * (i - divide) / (SEGMENTS - divide);
                    }
                    drawLine(mRect1[j], mRect3[j]);
                    drawLine(mRect2[j], mRect4[j]);
                }
                drawLine(mRect1[SEGMENTS + 1], mRect2[SEGMENTS + 1]);
            }

            /* draw slider */
            drawLine(ce.Post[2], mCorner2);
            drawLine(mCorner2, mArrowPoint);
            drawLine(mArrow1, mArrowPoint);
            drawLine(mArrow2, mArrowPoint);

            /* draw dot */
            updateDotCount(ce.Current1, ref ce.CurCount1);
            updateDotCount(ce.Current2, ref ce.CurCount2);
            updateDotCount(ce.Current3, ref ce.CurCount3);
            if (CirSimForm.DragElm != this) {
                drawCurrent(ce.Post[0], mMidPoint, ce.CurCount1);
                drawCurrent(ce.Post[1], mMidPoint, ce.CurCount2);
                drawCurrent(ce.Post[2], mCorner2, ce.CurCount3);
                drawCurrent(mCorner2, mMidPoint, ce.CurCount3 + Utils.Distance(ce.Post[2], mCorner2));
            }
            drawPosts();

            if (ControlPanel.ChkShowValues.Checked && ce.Resistance1 > 0 && (DumpInfo.Flags & FLAG_SHOW_VALUES) != 0) {
                /* check for vertical pot with 3rd terminal on left */
                bool reverseY = (ce.Post[2].X < mLead1.X && mLead1.X == mLead2.X);
                /* check for horizontal pot with 3rd terminal on top */
                bool reverseX = (ce.Post[2].Y < mLead1.Y && mLead1.X != mLead2.X);
                /* check if we need to swap texts (if leads are reversed, e.g. drawn right to left) */
                bool rev = (mLead1.X == mLead2.X && mLead1.Y < mLead2.Y) || (mLead1.Y == mLead2.Y && mLead1.X > mLead2.X);

                /* draw units */
                var s1 = Utils.UnitText(rev ? ce.Resistance2 : ce.Resistance1, "");
                var s2 = Utils.UnitText(rev ? ce.Resistance1 : ce.Resistance2, "");
                var txtHeightHalf = CustomGraphics.TextSize * 0.5f;
                var txtWidth1 = (int)g.GetTextSize(s1).Width;
                var txtWidth2 = (int)g.GetTextSize(s2).Width;

                /* vertical? */
                if (mLead1.X == mLead2.X) {
                    g.DrawLeftText(s1, reverseY ? (mArrowPoint.X - txtWidth1) : mArrowPoint.X, (int)(Math.Min(mArrow1.Y, mArrow2.Y) + txtHeightHalf * 3));
                    g.DrawLeftText(s2, reverseY ? (mArrowPoint.X - txtWidth2) : mArrowPoint.X, (int)(Math.Max(mArrow1.Y, mArrow2.Y) - txtHeightHalf * 3));
                } else {
                    var y = (int)(mArrowPoint.Y + (reverseX ? -txtHeightHalf : txtHeightHalf));
                    g.DrawLeftText(s1, Math.Min(mArrow1.X, mArrow2.X) - txtWidth1, y);
                    g.DrawLeftText(s2, Math.Max(mArrow1.X, mArrow2.X), y);
                }
            }
            if (ControlPanel.ChkShowName.Checked) {
                if (mVertical) {
                    g.DrawCenteredVText(DumpInfo.ReferenceName, mNamePos);
                } else {
                    g.DrawLeftText(DumpInfo.ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmPot)Elm;
            arr[0] = "可変抵抗";
            arr[1] = "Vd = " + Utils.VoltageAbsText(ce.GetVoltageDiff());
            arr[2] = "R1 = " + Utils.UnitText(ce.Resistance1, CirSimForm.OHM_TEXT);
            arr[3] = "R2 = " + Utils.UnitText(ce.Resistance2, CirSimForm.OHM_TEXT);
            arr[4] = "I1 = " + Utils.CurrentAbsText(ce.Current1);
            arr[5] = "I2 = " + Utils.CurrentAbsText(ce.Current2);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmPot)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("レジスタンス(Ω)", ce.MaxResistance);
            }
            if (r == 1) {
                return new ElementInfo("名前", DumpInfo.ReferenceName);
            }
            if (r == 2) {
                return new ElementInfo("値を表示", (DumpInfo.Flags & FLAG_SHOW_VALUES) != 0);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmPot)Elm;
            if (n == 0) {
                ce.MaxResistance = ei.Value;
            }
            if (n == 1) {
                DumpInfo.ReferenceName = ei.Text;
                mLabel.Text = DumpInfo.ReferenceName;
                ControlPanel.SetSliderPanelHeight();
                setNamePos();
            }
            if (n == 2) {
                DumpInfo.Flags = ei.ChangeFlag(DumpInfo.Flags, FLAG_SHOW_VALUES);
            }
        }

        void setPoly() {
            /* set zigzag */
            int oy = 0;
            int ny;
            mPs1 = new Point[SEGMENTS + 1];
            mPs2 = new Point[SEGMENTS + 1];
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
            mRect1 = new Point[SEGMENTS + 2];
            mRect2 = new Point[SEGMENTS + 2];
            mRect3 = new Point[SEGMENTS + 2];
            mRect4 = new Point[SEGMENTS + 2];
            interpLeadAB(ref mRect1[0], ref mRect2[0], 0, HS);
            for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
                interpLeadAB(ref mRect1[j], ref mRect2[j], i * SEG_F, HS);
                interpLeadAB(ref mRect3[j], ref mRect4[j], (i + 1) * SEG_F, HS);
            }
            interpLeadAB(ref mRect1[SEGMENTS + 1], ref mRect2[SEGMENTS + 1], 1, HS);
        }

        void setNamePos() {
            var wn = Context.GetTextSize(DumpInfo.ReferenceName).Width * 0.5;
            if (Math.Abs(mDiff.Y) < Math.Abs(mDiff.X)) {
                if (0 < mDiff.X) {
                    /* upper slider */
                    interpPost(ref mNamePos, 0.5 + wn / mLen * mDsign, 14 * mDsign);
                } else {
                    /* lower slider */
                    interpPost(ref mNamePos, 0.5 + wn / mLen * mDsign, -12 * mDsign);
                }
            } else {
                if (mDiff.Y != 0) {
                    if (0 < mDiff.Y) {
                        /* right slider */
                        interpPost(ref mNamePos, 0.5, -20 * mDsign);
                    } else {
                        /* left slider */
                        interpPost(ref mNamePos, 0.5, 2 * mDsign);
                    }
                }
            }
        }

        void createSlider() {
            var ce = (ElmPot)Elm;
            ControlPanel.AddSlider(mLabel = new Label() {
                TextAlign = ContentAlignment.BottomLeft,
                Text = DumpInfo.ReferenceName
            });
            int value = (int)(ce.Position * 100);
            ControlPanel.AddSlider(mSlider = new TrackBar() {
                Minimum = 0,
                Maximum = 100,
                SmallChange = 1,
                LargeChange = 5,
                TickFrequency = 10,
                Value = value,
                Width = 175
            });
            mSlider.ValueChanged += new EventHandler((s, e) => { execute(); });
        }

        void execute() {
            SetPoints();
            CirSimForm.NeedAnalyze();
        }
    }
}
