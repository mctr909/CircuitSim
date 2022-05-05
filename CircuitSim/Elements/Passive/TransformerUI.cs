using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Passive {
    class TransformerUI : BaseUI {
        public const int FLAG_REVERSE = 4;

        const int BODY_LEN = 24;

        Point[] mPtEnds;
        Point[] mPtCoil;
        Point[] mPtCore;
        Point[] mDots;

        public TransformerUI(Point pos) : base(pos) {
            CirElm = new TransformerElm();
            mNoDiagonal = true;
            ReferenceName = "T";
        }

        public TransformerUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new TransformerElm(st, (mFlags & FLAG_REVERSE) != 0);
            try {
                ReferenceName = st.nextToken();
            } catch { }
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRANSFORMER; } }

        protected override string dump() {
            var ce = (TransformerElm)CirElm;
            return ce.PInductance
                + " " + ce.Ratio
                + " " + ce.Currents[0]
                + " " + ce.Currents[1]
                + " " + ce.CouplingCoef
                + " " + ReferenceName;
        }

        public override Point GetPost(int n) {
            return mPtEnds[n];
        }

        public override bool GetConnection(int n1, int n2) {
            if (comparePair(n1, n2, 0, 2)) {
                return true;
            }
            if (comparePair(n1, n2, 1, 3)) {
                return true;
            }
            return false;
        }

        public override void Drag(Point pos) {
            pos = CirSim.Sim.SnapGrid(pos);
            P2.X = pos.X;
            P2.Y = pos.Y;
            SetPoints();
        }

        public override void SetPoints() {
            var elm = (TransformerElm)CirElm;
            var width = Math.Max(BODY_LEN, Math.Abs(P2.X - P1.X));
            var height = Math.Max(BODY_LEN, Math.Abs(P2.Y - P1.Y));
            if (P2.X == P1.X) {
                P2.Y = P1.Y;
            }
            base.SetPoints();
            mPoint2.Y = mPoint1.Y;
            mPtEnds = new Point[4];
            mPtCoil = new Point[4];
            mPtCore = new Point[4];
            mPtEnds[0] = mPoint1;
            mPtEnds[1] = mPoint2;
            interpPoint(ref mPtEnds[2], 0, -mDsign * height);
            interpPoint(ref mPtEnds[3], 1, -mDsign * height);
            var ce = 0.5 - 10.0 / width;
            var cd = 0.5 - 1.0 / width;
            for (int i = 0; i != 4; i += 2) {
                Utils.InterpPoint(mPtEnds[i], mPtEnds[i + 1], ref mPtCoil[i], ce);
                Utils.InterpPoint(mPtEnds[i], mPtEnds[i + 1], ref mPtCoil[i + 1], 1 - ce);
                Utils.InterpPoint(mPtEnds[i], mPtEnds[i + 1], ref mPtCore[i], cd);
                Utils.InterpPoint(mPtEnds[i], mPtEnds[i + 1], ref mPtCore[i + 1], 1 - cd);
            }
            if (-1 == elm.Polarity) {
                mDots = new Point[2];
                var dotp = Math.Abs(7.0 / height);
                Utils.InterpPoint(mPtCoil[0], mPtCoil[2], ref mDots[0], dotp, -7 * mDsign);
                Utils.InterpPoint(mPtCoil[3], mPtCoil[1], ref mDots[1], dotp, -7 * mDsign);
                var x = mPtEnds[1];
                mPtEnds[1] = mPtEnds[3];
                mPtEnds[3] = x;
                x = mPtCoil[1];
                mPtCoil[1] = mPtCoil[3];
                mPtCoil[3] = x;
            } else {
                mDots = null;
            }
            setNamePos();
        }

        public override void Draw(CustomGraphics g) {
            var ce = (TransformerElm)CirElm;

            drawLead(mPtEnds[0], mPtCoil[0]);
            drawLead(mPtEnds[1], mPtCoil[1]);
            drawLead(mPtEnds[2], mPtCoil[2]);
            drawLead(mPtEnds[3], mPtCoil[3]);

            drawCoil(mPtCoil[0], mPtCoil[2], ce.Volts[TransformerElm.PRI_T], ce.Volts[TransformerElm.PRI_B], 90 * mDsign);
            drawCoil(mPtCoil[1], mPtCoil[3], ce.Volts[TransformerElm.SEC_T], ce.Volts[TransformerElm.SEC_B], -90 * mDsign * ce.Polarity);

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawLine(mPtCore[0], mPtCore[2]);
            g.DrawLine(mPtCore[1], mPtCore[3]);
            if (mDots != null) {
                g.DrawCircle(mDots[0], 2.5f);
                g.DrawCircle(mDots[1], 2.5f);
            }

            ce.CurCounts[0] = updateDotCount(ce.Currents[0], ce.CurCounts[0]);
            ce.CurCounts[1] = updateDotCount(ce.Currents[1], ce.CurCounts[1]);
            for (int i = 0; i != 2; i++) {
                drawDots(mPtEnds[i], mPtCoil[i], ce.CurCounts[i]);
                drawDots(mPtCoil[i], mPtCoil[i + 2], ce.CurCounts[i]);
                drawDots(mPtEnds[i + 2], mPtCoil[i + 2], -ce.CurCounts[i]);
            }

            drawPosts();
            setBbox(mPtEnds[0], mPtEnds[ce.Polarity == 1 ? 3 : 1], 0);

            if (ControlPanel.ChkShowName.Checked) {
                g.DrawLeftText(ReferenceName, mNamePos.X, mNamePos.Y);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (TransformerElm)CirElm;
            arr[0] = "トランス";
            arr[1] = "L = " + Utils.UnitText(ce.PInductance, "H");
            arr[2] = "Ratio = 1:" + ce.Ratio;
            arr[3] = "Vd1 = " + Utils.VoltageText(ce.Volts[TransformerElm.PRI_T] - ce.Volts[TransformerElm.PRI_B]);
            arr[4] = "Vd2 = " + Utils.VoltageText(ce.Volts[TransformerElm.SEC_T] - ce.Volts[TransformerElm.SEC_B]);
            arr[5] = "I1 = " + Utils.CurrentText(ce.Currents[0]);
            arr[6] = "I2 = " + Utils.CurrentText(ce.Currents[1]);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (TransformerElm)CirElm;
            if (n == 0) {
                return new ElementInfo("一次側インダクタンス(H)", ce.PInductance, .01, 5);
            }
            if (n == 1) {
                return new ElementInfo("二次側巻数比", ce.Ratio, 1, 10).SetDimensionless();
            }
            if (n == 2) {
                return new ElementInfo("結合係数", ce.CouplingCoef, 0, 1).SetDimensionless();
            }
            if (n == 3) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = ReferenceName;
                return ei;
            }
            if (n == 4) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "極性反転",
                    Checked = ce.Polarity == -1
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (TransformerElm)CirElm;
            if (n == 0 && ei.Value > 0) {
                ce.PInductance = ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                ce.Ratio = ei.Value;
            }
            if (n == 2 && ei.Value > 0 && ei.Value < 1) {
                ce.CouplingCoef = ei.Value;
            }
            if (n == 3) {
                ReferenceName = ei.Textf.Text;
                setNamePos();
            }
            if (n == 4) {
                ce.Polarity = ei.CheckBox.Checked ? -1 : 1;
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_REVERSE;
                } else {
                    mFlags &= ~FLAG_REVERSE;
                }
                SetPoints();
            }
        }

        void setNamePos() {
            var wn = Context.GetTextSize(ReferenceName).Width;
            mNamePos = new Point((int)(mPtCore[0].X - wn / 2 + 2), mPtCore[0].Y - 8);
        }
    }
}
