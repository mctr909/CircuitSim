using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Input {
    class SweepUI : BaseUI {
        const int FLAG_LOG = 1;
        const int FLAG_BIDIR = 2;

        const int SIZE = 28;

        Point mTextPos;

        public SweepUI(Point pos) : base(pos) {
            CirElm = new SweepElm();
            mFlags = FLAG_BIDIR;
            ((SweepElm)CirElm).BothSides = 0 != (mFlags & FLAG_BIDIR);
        }

        public SweepUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new SweepElm(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.SWEEP; } }

        protected override string dump() {
            var ce = (SweepElm)CirElm;
            return ce.MinF
                + " " + ce.MaxF
                + " " + ce.MaxV
                + " " + ce.SweepTime;
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 0.5 * SIZE / mLen);
            interpPoint(ref mTextPos, 1.0 + 0.66 * SIZE / Utils.Distance(mPost1, mPost2), 24 * mDsign);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (SweepElm)CirElm;
            setBbox(mPost1, mPost2, SIZE);

            drawLead(mPost1, mLead1);

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;

            int xc = mPost2.X;
            int yc = mPost2.Y;
            g.DrawCircle(mPost2, SIZE / 2);

            adjustBbox(
                xc - SIZE, yc - SIZE,
                xc + SIZE, yc + SIZE
            );

            int wl = 7;
            int xl = 10;
            long tm = DateTime.Now.ToFileTimeUtc();
            tm %= 2000;
            if (tm > 1000) {
                tm = 2000 - tm;
            }
            double w = 1 + tm * 0.002;
            if (CirSim.Sim.IsRunning) {
                w = 1 + 3 * (ce.Frequency - ce.MinF) / (ce.MaxF - ce.MinF);
            }

            int x0 = 0;
            int y0 = 0;
            g.LineColor = CustomGraphics.GrayColor;
            for (int i = -xl; i <= xl; i++) {
                var yy = yc + (int)(0.95 * Math.Sin(i * Math.PI * w / xl) * wl);
                if (i == -xl) {
                    x0 = xc + i;
                    y0 = yy;
                } else {
                    g.DrawLine(x0, y0, xc + i, yy);
                    x0 = xc + i;
                    y0 = yy;
                }
            }

            if (ControlPanel.ChkShowValues.Checked) {
                string s = Utils.UnitText(ce.Frequency, "Hz");
                drawValues(s, 20, -15);
            }

            drawPosts();
            ce.CurCount = updateDotCount(-ce.Current, ce.CurCount);
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPost1, mLead1, ce.CurCount);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (SweepElm)CirElm;
            arr[0] = "sweep " + (((mFlags & FLAG_LOG) == 0) ? "(linear)" : "(log)");
            arr[1] = "I = " + Utils.CurrentAbsText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.Volts[0]);
            arr[3] = "f = " + Utils.UnitText(ce.Frequency, "Hz");
            arr[4] = "range = " + Utils.UnitText(ce.MinF, "Hz") + " .. " + Utils.UnitText(ce.MaxF, "Hz");
            arr[5] = "time = " + Utils.UnitText(ce.SweepTime, "s");
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (SweepElm)CirElm;
            if (n == 0) {
                return new ElementInfo("振幅(V)", ce.MaxV, 0, 0);
            }
            if (n == 1) {
                return new ElementInfo("最小周波数(Hz)", ce.MinF, 0, 0);
            }
            if (n == 2) {
                return new ElementInfo("最大周波数(Hz)", ce.MaxF, 0, 0);
            }
            if (n == 3) {
                return new ElementInfo("スウィープ時間(sec)", ce.SweepTime, 0, 0);
            }
            if (n == 4) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "周波数対数変化",
                    Checked = (mFlags & FLAG_LOG) != 0
                };
                return ei;
            }
            if (n == 5) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "双方向周波数遷移",
                    Checked = (mFlags & FLAG_BIDIR) != 0
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (SweepElm)CirElm;
            double maxfreq = 1 / (8 * ControlPanel.TimeStep);
            if (n == 0) {
                ce.MaxV = ei.Value;
            }
            if (n == 1) {
                ce.MinF = ei.Value;
                if (ce.MinF > maxfreq) {
                    ce.MinF = maxfreq;
                }
            }
            if (n == 2) {
                ce.MaxF = ei.Value;
                if (ce.MaxF > maxfreq) {
                    ce.MaxF = maxfreq;
                }
            }
            if (n == 3) {
                ce.SweepTime = ei.Value;
            }
            if (n == 4) {
                mFlags &= ~FLAG_LOG;
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_LOG;
                }
                ce.IsLog = 0 != (mFlags & FLAG_LOG);
            }
            if (n == 5) {
                mFlags &= ~FLAG_BIDIR;
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_BIDIR;
                }
                ce.BothSides = 0 != (mFlags & FLAG_BIDIR);
            }
            ce.setParams();
        }
    }
}
