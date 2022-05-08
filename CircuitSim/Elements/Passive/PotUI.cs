﻿using System;
using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class PotUI : BaseUI {
        const int FLAG_SHOW_VALUES = 1;

        const int HS = 5;
        const int BODY_LEN = 24;
        const int SEGMENTS = 12;
        const double SEG_F = 1.0 / SEGMENTS;

        Point mPost3;
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

        public PotUI(Point pos) : base(pos) {
            Elm = new PotElm();
            mFlags = FLAG_SHOW_VALUES;
            ReferenceName = "VR";
            createSlider();
        }

        public PotUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                Elm = new PotElm(st);
                ReferenceName = st.nextToken();
                while (st.hasMoreTokens()) {
                    ReferenceName += ' ' + st.nextToken();
                }
            } catch (Exception ex) {
                throw new Exception("Pot load error:{0}", ex);
            }
            createSlider();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.POT; } }

        protected override string dump() {
            var ce = (PotElm)Elm;
            return ce.MaxResistance + " " + ce.Position + " " + ReferenceName;
        }

        public override void SetMouseElm(bool v) {
            base.SetMouseElm(v);
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPost1 : (n == 1) ? mPost2 : mPost3;
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
            mNameV = Math.Abs(mDiff.X) <= Math.Abs(mDiff.Y);
            if (mNameV) {
                /* vertical */
                myLen = 2 * CirSim.GRID_SIZE * Math.Sign(mDiff.Y)
                    * ((Math.Abs(mDiff.Y) + 2 * CirSim.GRID_SIZE - 1) / (2 * CirSim.GRID_SIZE));
                if (mDiff.Y != 0) {
                    mPost2.Y = mPost1.Y + myLen;
                    offset = (0 < mDiff.Y) ? mDiff.X : -mDiff.X;
                    mPost2.X = mPost1.X;
                }
            } else {
                /* horizontal */
                myLen = 2 * CirSim.GRID_SIZE * Math.Sign(mDiff.X)
                    * ((Math.Abs(mDiff.X) + 2 * CirSim.GRID_SIZE - 1) / (2 * CirSim.GRID_SIZE));
                mPost2.X = mPost1.X + myLen;
                offset = (mDiff.X < 0) ? mDiff.Y : -mDiff.Y;
                mPost2.Y = mPost1.Y;
            }
            if (offset < CirSim.GRID_SIZE) {
                offset = CirSim.GRID_SIZE;
            }
            mLen = Utils.Distance(mPost1, mPost2);

            calcLeads(BODY_LEN);

            /* set slider */
            var ce = (PotElm)Elm;
            ce.Position = mSlider.Value * 0.0099 + 0.0001;
            int soff = (int)((ce.Position - 0.5) * BODY_LEN);
            interpPoint(ref mPost3, 0.5, offset);
            interpPoint(ref mCorner2, soff / mLen + 0.5, offset);
            interpPoint(ref mArrowPoint, soff / mLen + 0.5, 8 * Math.Sign(offset));
            interpPoint(ref mMidPoint, soff / mLen + 0.5);
            double clen = Math.Abs(offset) - 8;
            Utils.InterpPoint(mCorner2, mArrowPoint, ref mArrow1, ref mArrow2, (clen - 8) / clen, 4);

            setNamePos();

            setPoly();
        }

        public override void Draw(CustomGraphics g) {
            var ce = (PotElm)Elm;
            double vl = ce.Volts[PotElm.V_L];
            double vr = ce.Volts[PotElm.V_R];
            double vs = ce.Volts[PotElm.V_S];
            setBbox(mPost1, mPost2, HS);
            draw2Leads();

            int divide = (int)(SEGMENTS * ce.Position);

            if (ControlPanel.ChkUseAnsiSymbols.Checked) {
                /* draw zigzag */
                for (int i = 0; i != SEGMENTS; i++) {
                    double v = vl + (vs - vl) * i / divide;
                    if (i >= divide) {
                        v = vs + (vr - vs) * (i - divide) / (SEGMENTS - divide);
                    }
                    drawLead(mPs1[i], mPs2[i]);
                }
            } else {
                /* draw rectangle */
                drawLead(mRect1[0], mRect2[0]);
                for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
                    double v = vl + (vs - vl) * i / divide;
                    if (i >= divide) {
                        v = vs + (vr - vs) * (i - divide) / (SEGMENTS - divide);
                    }
                    drawLead(mRect1[j], mRect3[j]);
                    drawLead(mRect2[j], mRect4[j]);
                }
                drawLead(mRect1[SEGMENTS + 1], mRect2[SEGMENTS + 1]);
            }

            /* draw slider */
            drawLead(mPost3, mCorner2);
            drawLead(mCorner2, mArrowPoint);
            drawLead(mArrow1, mArrowPoint);
            drawLead(mArrow2, mArrowPoint);

            /* draw dot */
            ce.CurCount1 = updateDotCount(ce.Current1, ce.CurCount1);
            ce.CurCount2 = updateDotCount(ce.Current2, ce.CurCount2);
            ce.CurCount3 = updateDotCount(ce.Current3, ce.CurCount3);
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPost1, mMidPoint, ce.CurCount1);
                drawDots(mPost2, mMidPoint, ce.CurCount2);
                drawDots(mPost3, mCorner2, ce.CurCount3);
                drawDots(mCorner2, mMidPoint, ce.CurCount3 + Utils.Distance(mPost3, mCorner2));
            }
            drawPosts();

            if (ControlPanel.ChkShowValues.Checked && ce.Resistance1 > 0 && (mFlags & FLAG_SHOW_VALUES) != 0) {
                /* check for vertical pot with 3rd terminal on left */
                bool reverseY = (mPost3.X < mLead1.X && mLead1.X == mLead2.X);
                /* check for horizontal pot with 3rd terminal on top */
                bool reverseX = (mPost3.Y < mLead1.Y && mLead1.X != mLead2.X);
                /* check if we need to swap texts (if leads are reversed, e.g. drawn right to left) */
                bool rev = (mLead1.X == mLead2.X && mLead1.Y < mLead2.Y) || (mLead1.Y == mLead2.Y && mLead1.X > mLead2.X);

                /* draw units */
                string s1 = Utils.UnitText(rev ? ce.Resistance2 : ce.Resistance1, "");
                string s2 = Utils.UnitText(rev ? ce.Resistance1 : ce.Resistance2, "");
                int txtHeightH = CustomGraphics.FontText.Height / 2;
                int txtWidth1 = (int)g.GetTextSize(s1).Width;
                int txtWidth2 = (int)g.GetTextSize(s2).Width;

                /* vertical? */
                if (mLead1.X == mLead2.X) {
                    g.DrawLeftTopText(s1, !reverseY ? mArrowPoint.X : mArrowPoint.X - txtWidth1, Math.Min(mArrow1.Y, mArrow2.Y) + 4 * txtHeightH);
                    g.DrawLeftTopText(s2, !reverseY ? mArrowPoint.X : mArrowPoint.X - txtWidth2, Math.Max(mArrow1.Y, mArrow2.Y) - txtHeightH);
                } else {
                    g.DrawLeftTopText(s1, Math.Min(mArrow1.X, mArrow2.X) - txtWidth1, !reverseX ? (mArrowPoint.Y + txtHeightH + 10) : mArrowPoint.Y);
                    g.DrawLeftTopText(s2, Math.Max(mArrow1.X, mArrow2.X), !reverseX ? (mArrowPoint.Y + txtHeightH + 10) : mArrowPoint.Y);
                }
            }
            if (ControlPanel.ChkShowName.Checked) {
                if (mNameV) {
                    g.DrawCenteredVText(ReferenceName, mNamePos.X, mNamePos.Y);
                } else {
                    g.DrawLeftText(ReferenceName, mNamePos.X, mNamePos.Y);
                }
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (PotElm)Elm;
            arr[0] = "可変抵抗";
            arr[1] = "Vd = " + Utils.VoltageAbsText(ce.VoltageDiff);
            arr[2] = "R1 = " + Utils.UnitText(ce.Resistance1, CirSim.OHM_TEXT);
            arr[3] = "R2 = " + Utils.UnitText(ce.Resistance2, CirSim.OHM_TEXT);
            arr[4] = "I1 = " + Utils.CurrentAbsText(ce.Current1);
            arr[5] = "I2 = " + Utils.CurrentAbsText(ce.Current2);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (PotElm)Elm;
            if (n == 0) {
                return new ElementInfo("レジスタンス(Ω)", ce.MaxResistance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名前", 0, -1, -1);
                ei.Text = ReferenceName;
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "値を表示";
                ei.CheckBox.Checked = (mFlags & FLAG_SHOW_VALUES) != 0;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (PotElm)Elm;
            if (n == 0) {
                ce.MaxResistance = ei.Value;
            }
            if (n == 1) {
                ReferenceName = ei.Textf.Text;
                mLabel.Text = ReferenceName;
                ControlPanel.SetSliderPanelHeight();
                setNamePos();
            }
            if (n == 2) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_SHOW_VALUES);
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
            var wn = Context.GetTextSize(ReferenceName).Width * 0.5;
            if (Math.Abs(mDiff.Y) < Math.Abs(mDiff.X)) {
                if (0 < mDiff.X) {
                    /* upper slider */
                    interpPoint(ref mNamePos, 0.5 + wn / mLen * mDsign, 14 * mDsign);
                } else {
                    /* lower slider */
                    interpPoint(ref mNamePos, 0.5 + wn / mLen * mDsign, -12 * mDsign);
                }
            } else {
                if (mDiff.Y != 0) {
                    if (0 < mDiff.Y) {
                        /* right slider */
                        interpPoint(ref mNamePos, 0.5, -20 * mDsign);
                    } else {
                        /* left slider */
                        interpPoint(ref mNamePos, 0.5, 2 * mDsign);
                    }
                }
            }
        }

        void createSlider() {
            var ce = (PotElm)Elm;
            ControlPanel.AddSlider(mLabel = new Label() {
                TextAlign = ContentAlignment.BottomLeft,
                Text = ReferenceName
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
            CirSim.Sim.NeedAnalyze();
        }
    }
}