using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class Sweep : BaseUI {
        const int FLAG_LOG = 1;
        const int FLAG_BIDIR = 2;

        const int SIZE = 32;

        public Sweep(Point pos) : base(pos) {
            Elm = new ElmSweep();
            mFlags = FLAG_BIDIR;
            ((ElmSweep)Elm).BothSides = 0 != (mFlags & FLAG_BIDIR);
        }

        public Sweep(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmSweep(st);
            var ce = (ElmSweep)Elm;
            ce.IsLog = 0 != (mFlags & FLAG_LOG);
            ce.BothSides = 0 != (mFlags & FLAG_BIDIR);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.SWEEP; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmSweep)Elm;
            optionList.Add(ce.MinF.ToString("g3"));
            optionList.Add(ce.MaxF.ToString("g3"));
            optionList.Add(ce.MaxV.ToString("g3"));
            optionList.Add(ce.SweepTime.ToString("g3"));
        }

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(SIZE);
            Post.AdjustBbox(
                Elm.Post[1].X - SIZE, Elm.Post[1].Y - SIZE,
                Elm.Post[1].X + SIZE, Elm.Post[1].Y + SIZE
            );
            setLead1(1 - 0.5 * SIZE / Post.Len);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmSweep)Elm;
            
            drawLeadA();

            int xc = Elm.Post[1].X;
            int yc = Elm.Post[1].Y;
            drawCircle(Elm.Post[1], SIZE / 2);

            int wl = 11;
            int xl = 10;
            long tm = DateTime.Now.ToFileTimeUtc();
            tm %= 2000;
            if (tm > 1000) {
                tm = 2000 - tm;
            }
            double w = 1 + tm * 0.002;
            if (CirSimForm.IsRunning) {
                w = 1.01 + (ce.Frequency - ce.MinF) / (ce.MaxF - ce.MinF);
            }

            int x0 = 0;
            var y0 = 0.0f;
            for (int i = -xl; i <= xl; i++) {
                var yy = yc + (float)(0.95 * Math.Sin(i * Math.PI * w / xl) * wl);
                if (i == -xl) {
                    x0 = xc + i;
                    y0 = yy;
                } else {
                    drawLine(x0, y0, xc + i, yy);
                    x0 = xc + i;
                    y0 = yy;
                }
            }

            if (ControlPanel.ChkShowValues.Checked) {
                string s = Utils.UnitText(ce.Frequency, "Hz");
                drawValues(s, 20, -15);
            }

            updateDotCount(-ce.Current, ref mCurCount);
            if (CirSimForm.ConstructElm != this) {
                drawCurrentA(mCurCount);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmSweep)Elm;
            arr[0] = "sweep " + (((mFlags & FLAG_LOG) == 0) ? "(linear)" : "(log)");
            arr[1] = "I = " + Utils.CurrentAbsText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.Volts[0]);
            arr[3] = "f = " + Utils.UnitText(ce.Frequency, "Hz");
            arr[4] = "range = " + Utils.UnitText(ce.MinF, "Hz") + " .. " + Utils.UnitText(ce.MaxF, "Hz");
            arr[5] = "time = " + Utils.UnitText(ce.SweepTime, "s");
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmSweep)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("振幅", ce.MaxV);
            }
            if (r == 1) {
                return new ElementInfo("最小周波数", ce.MinF);
            }
            if (r == 2) {
                return new ElementInfo("最大周波数", ce.MaxF);
            }
            if (r == 3) {
                return new ElementInfo("スウィープ時間(sec)", ce.SweepTime);
            }
            if (r == 4) {
                return new ElementInfo("周波数対数変化", (mFlags & FLAG_LOG) != 0);
            }
            if (r == 5) {
                return new ElementInfo("双方向周波数遷移", (mFlags & FLAG_BIDIR) != 0);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmSweep)Elm;
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
