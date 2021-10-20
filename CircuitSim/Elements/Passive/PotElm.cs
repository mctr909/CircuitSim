using System;
using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class PotElm : CircuitElm {
        const int FLAG_SHOW_VALUES = 1;
        const int V_L = 0;
        const int V_R = 1;
        const int V_S = 2;

        const int HS = 5;
        const int BODY_LEN = 24;
        const int SEGMENTS = 12;
        const double SEG_F = 1.0 / SEGMENTS;

        double mPosition;
        double mMaxResistance;
        double mResistance1;
        double mResistance2;
        double mCurrent1;
        double mCurrent2;
        double mCurrent3;
        double mCurCount1;
        double mCurCount2;
        double mCurCount3;

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

        Point mNamePos;
        string mReferenceName;

        TrackBar mSlider;
        Label mLabel;

        public PotElm(Point pos) : base(pos) {
            setup();
            mMaxResistance = 1000;
            mPosition = .5;
            mReferenceName = "VR";
            mFlags = FLAG_SHOW_VALUES;
            createSlider();
        }

        public PotElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                mMaxResistance = st.nextTokenDouble();
                mPosition = st.nextTokenDouble();
                mReferenceName = st.nextToken();
                while (st.hasMoreTokens()) {
                    mReferenceName += ' ' + st.nextToken();
                }
            } catch { }
            createSlider();
        }

        public override int PostCount { get { return 3; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.POT; } }

        protected override string dump() {
            return mMaxResistance + " " + mPosition + " " + mReferenceName;
        }

        protected override void calculateCurrent() {
            if (mResistance1 == 0) {
                return; /* avoid NaN */
            }
            mCurrent1 = (Volts[V_L] - Volts[V_S]) / mResistance1;
            mCurrent2 = (Volts[V_R] - Volts[V_S]) / mResistance2;
            mCurrent3 = -mCurrent1 - mCurrent2;
        }

        void setup() { }

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

        public override void SetMouseElm(bool v) {
            base.SetMouseElm(v);
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mPoint2 : mPost3;
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCurrent1;
            }
            if (n == 1) {
                return -mCurrent2;
            }
            return -mCurrent3;
        }

        public override void Delete() {
            ControlPanel.RemoveSlider(mLabel);
            ControlPanel.RemoveSlider(mSlider);
            base.Delete();
        }

        public override void Stamp() {
            mResistance1 = mMaxResistance * mPosition;
            mResistance2 = mMaxResistance * (1 - mPosition);
            mCir.StampResistor(Nodes[0], Nodes[2], mResistance1);
            mCir.StampResistor(Nodes[2], Nodes[1], mResistance2);
        }

        public override void SetPoints() {
            base.SetPoints();
            int offset = 0;
            int myLen = 0;
            if (Math.Abs(mDiff.Y) < Math.Abs(mDiff.X)) {
                /* horizontal */
                myLen = 2 * CirSim.GRID_SIZE * Math.Sign(mDiff.X)
                    * ((Math.Abs(mDiff.X) + 2 * CirSim.GRID_SIZE - 1) / (2 * CirSim.GRID_SIZE));
                mPoint2.X = mPoint1.X + myLen;
                offset = (mDiff.X < 0) ? mDiff.Y : -mDiff.Y;
                mPoint2.Y = mPoint1.Y;
            } else {
                /* vertical */
                myLen = 2 * CirSim.GRID_SIZE * Math.Sign(mDiff.Y)
                    * ((Math.Abs(mDiff.Y) + 2 * CirSim.GRID_SIZE - 1) / (2 * CirSim.GRID_SIZE));
                if (mDiff.Y != 0) {
                    mPoint2.Y = mPoint1.Y + myLen;
                    offset = (0 < mDiff.Y) ? mDiff.X : -mDiff.X;
                    mPoint2.X = mPoint1.X;
                }
            }
            if (offset < CirSim.GRID_SIZE) {
                offset = CirSim.GRID_SIZE;
            }
            mLen = Utils.Distance(mPoint1, mPoint2);

            calcLeads(BODY_LEN);

            /* set slider */
            mPosition = mSlider.Value * 0.0099 + 0.0001;
            int soff = (int)((mPosition - 0.5) * BODY_LEN);
            interpPoint(ref mPost3, 0.5, offset);
            interpPoint(ref mCorner2, soff / mLen + 0.5, offset);
            interpPoint(ref mArrowPoint, soff / mLen + 0.5, 8 * Math.Sign(offset));
            interpPoint(ref mMidPoint, soff / mLen + 0.5);
            double clen = Math.Abs(offset) - 8;
            Utils.InterpPoint(mCorner2, mArrowPoint, ref mArrow1, ref mArrow2, (clen - 8) / clen, 4);

            setNamePos();

            setPoly();
        }

        void setNamePos() {
            var wn = Context.GetTextSize(mReferenceName).Width * 0.5;
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
                        interpPoint(ref mNamePos, 0.5, -(2 * wn + 4) * mDsign);
                    } else {
                        /* left slider */
                        interpPoint(ref mNamePos, 0.5, 5 * mDsign);
                    }
                }
            }
        }

        public override void Reset() {
            mCurCount1 = mCurCount2 = mCurCount3 = 0;
            base.Reset();
        }

        public override void Draw(CustomGraphics g) {
            double vl = Volts[V_L];
            double vr = Volts[V_R];
            double vs = Volts[V_S];
            setBbox(mPoint1, mPoint2, HS);
            draw2Leads();

            int divide = (int)(SEGMENTS * mPosition);

            if (ControlPanel.ChkUseAnsiSymbols.Checked) {
                /* draw zigzag */
                for (int i = 0; i != SEGMENTS; i++) {
                    double v = vl + (vs - vl) * i / divide;
                    if (i >= divide) {
                        v = vs + (vr - vs) * (i - divide) / (SEGMENTS - divide);
                    }
                    g.LineColor = getVoltageColor(v);
                    g.DrawLine(mPs1[i], mPs2[i]);
                }
            } else {
                /* draw rectangle */
                g.LineColor = getVoltageColor(vl);
                g.DrawLine(mRect1[0], mRect2[0]);
                for (int i = 0, j = 1; i != SEGMENTS; i++, j++) {
                    double v = vl + (vs - vl) * i / divide;
                    if (i >= divide) {
                        v = vs + (vr - vs) * (i - divide) / (SEGMENTS - divide);
                    }
                    g.LineColor = getVoltageColor(v);
                    g.DrawLine(mRect1[j], mRect3[j]);
                    g.DrawLine(mRect2[j], mRect4[j]);
                }
                g.DrawLine(mRect1[SEGMENTS + 1], mRect2[SEGMENTS + 1]);
            }

            /* draw slider */
            g.LineColor = getVoltageColor(vs);
            g.DrawLine(mPost3, mCorner2);
            g.DrawLine(mCorner2, mArrowPoint);
            g.DrawLine(mArrow1, mArrowPoint);
            g.DrawLine(mArrow2, mArrowPoint);

            /* draw dot */
            mCurCount1 = updateDotCount(mCurrent1, mCurCount1);
            mCurCount2 = updateDotCount(mCurrent2, mCurCount2);
            mCurCount3 = updateDotCount(mCurrent3, mCurCount3);
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mMidPoint, mCurCount1);
                drawDots(mPoint2, mMidPoint, mCurCount2);
                drawDots(mPost3, mCorner2, mCurCount3);
                drawDots(mCorner2, mMidPoint, mCurCount3 + Utils.Distance(mPost3, mCorner2));
            }
            drawPosts();

            if (ControlPanel.ChkShowValues.Checked && mResistance1 > 0 && (mFlags & FLAG_SHOW_VALUES) != 0) {
                /* check for vertical pot with 3rd terminal on left */
                bool reverseY = (mPost3.X < mLead1.X && mLead1.X == mLead2.X);
                /* check for horizontal pot with 3rd terminal on top */
                bool reverseX = (mPost3.Y < mLead1.Y && mLead1.X != mLead2.X);
                /* check if we need to swap texts (if leads are reversed, e.g. drawn right to left) */
                bool rev = (mLead1.X == mLead2.X && mLead1.Y < mLead2.Y) || (mLead1.Y == mLead2.Y && mLead1.X > mLead2.X);

                /* draw units */
                string s1 = Utils.ShortUnitText(rev ? mResistance2 : mResistance1, "");
                string s2 = Utils.ShortUnitText(rev ? mResistance1 : mResistance2, "");
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
            if (ControlPanel.ChkShowValues.Checked) {
                g.DrawLeftText(mReferenceName, mNamePos.X, mNamePos.Y);
            }
        }

        /* draw component values (number of resistor ohms, etc).  hs = offset */
        void drawValues(CustomGraphics g, string s, Point pt, int hs) {
            if (s == null) {
                return;
            }
            int w = (int)g.GetLTextSize(s).Width;
            int ya = CustomGraphics.FontText.Height / 2;
            int xc = pt.X;
            int yc = pt.Y;
            int dpx = hs;
            int dpy = 0;
            if (mLead1.X != mLead2.X) {
                dpx = 0;
                dpy = -hs;
            }
            Console.WriteLine("dv " + dpx + " " + w);
            if (dpx == 0) {
                g.DrawLeftText(s, xc - w / 2, yc - Math.Abs(dpy) - 2);
            } else {
                int xx = xc + Math.Abs(dpx) + 2;
                g.DrawLeftText(s, xx, yc + dpy + ya);
            }
        }

        void createSlider() {
            ControlPanel.AddSlider(mLabel = new Label() {
                TextAlign = ContentAlignment.BottomLeft,
                Text = mReferenceName
            });
            int value = (int)(mPosition * 100);
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

        public override void GetInfo(string[] arr) {
            arr[0] = "可変抵抗";
            arr[1] = "Vd = " + Utils.VoltageDText(VoltageDiff);
            arr[2] = "R1 = " + Utils.UnitText(mResistance1, CirSim.OHM_TEXT);
            arr[3] = "R2 = " + Utils.UnitText(mResistance2, CirSim.OHM_TEXT);
            arr[4] = "I1 = " + Utils.CurrentDText(mCurrent1);
            arr[5] = "I2 = " + Utils.CurrentDText(mCurrent2);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("レジスタンス(Ω)", mMaxResistance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名称", 0, -1, -1);
                ei.Text = mReferenceName;
                setNamePos();
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
            if (n == 0) {
                mMaxResistance = ei.Value;
            }
            if (n == 1) {
                mReferenceName = ei.Textf.Text;
                mLabel.Text = mReferenceName;
                ControlPanel.SetSliderPanelHeight();
            }
            if (n == 2) {
                mFlags = ei.ChangeFlag(mFlags, FLAG_SHOW_VALUES);
            }
        }
    }
}
