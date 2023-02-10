using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class VoltMeter : BaseUI {
        const int FLAG_SHOWVOLTAGE = 1;

        protected Point mCenter;
        Point mPlusPoint;

        public VoltMeter(Point pos) : base(pos) {
            Elm = new ElmVoltMeter();
            /* default for new elements */
            DumpInfo.Flags = FLAG_SHOWVOLTAGE;
        }

        public VoltMeter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmVoltMeter(st);
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.VOLTMETER; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTMETER; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmVoltMeter)Elm;
            optionList.Add(ce.Meter);
            optionList.Add(ce.Scale);
        }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mCenter, 0.5);
            interpPoint(ref mPlusPoint, 8.0 / mLen, 6 * mDsign);
        }

        public override void Draw(CustomGraphics g) {
            int hs = 8;
            setBbox(mPost1, mPost2, hs);
            bool selected = NeedsHighlight;
            double len = (selected || CirSimForm.DragElm == this || mustShowVoltage()) ? 16 : mLen - 32;
            calcLeads((int)len);

            if (selected) {
                g.DrawColor = CustomGraphics.SelectColor;
            } else {
                g.DrawColor = CustomGraphics.LineColor;
            }
            drawLead(mPost1, mLead1);

            if (selected) {
                g.DrawColor = CustomGraphics.SelectColor;
            } else {
                g.DrawColor = CustomGraphics.LineColor;
            }
            drawLead(mLead2, mPost2);

            if (this == CirSimForm.PlotXElm) {
                drawCenteredLText("X", mCenter, true);
            }
            if (this == CirSimForm.PlotYElm) {
                drawCenteredLText("Y", mCenter, true);
            }

            drawValues();
            drawCenteredLText("+", mPlusPoint, true);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmVoltMeter)Elm;
            arr[0] = "voltmeter";
            arr[1] = "Vd = " + Utils.VoltageText(ce.VoltageDiff);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmVoltMeter)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("表示", ce.Meter,
                    new string[] { "瞬時値", "実効値", "最大値", "最小値", "P-P", "2値" }
                );
            }
            if (r == 1) {
                return new ElementInfo("スケール", (int)ce.Scale, new string[] { "自動", "V", "mV", "uV" });
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmVoltMeter)Elm;
            if (n == 0) {
                ce.Meter = ei.Choice.SelectedIndex;
            }
            if (n == 1) {
                ce.Scale = (E_SCALE)ei.Choice.SelectedIndex;
            }
        }

        protected void drawValues() {
            if (mustShowVoltage()) {
                var ce = (ElmVoltMeter)Elm;
                string s = "";
                switch (ce.Meter) {
                case ElmVoltMeter.TP_VOL:
                    s = Utils.UnitTextWithScale(ce.VoltageDiff, "V", ce.Scale);
                    break;
                case ElmVoltMeter.TP_RMS:
                    s = Utils.UnitTextWithScale(ce.RmsV, "V(rms)", ce.Scale);
                    break;
                case ElmVoltMeter.TP_MAX:
                    s = Utils.UnitTextWithScale(ce.LastMaxV, "Vpk", ce.Scale);
                    break;
                case ElmVoltMeter.TP_MIN:
                    s = Utils.UnitTextWithScale(ce.LastMinV, "Vmin", ce.Scale);
                    break;
                case ElmVoltMeter.TP_P2P:
                    s = Utils.UnitTextWithScale(ce.LastMaxV - ce.LastMinV, "Vp2p", ce.Scale);
                    break;
                case ElmVoltMeter.TP_BIN:
                    s = ce.BinaryLevel + "";
                    break;
                }
                drawCenteredText(s, mCenter, true);
            }
        }

        bool mustShowVoltage() {
            return (DumpInfo.Flags & FLAG_SHOWVOLTAGE) != 0;
        }
    }
}
