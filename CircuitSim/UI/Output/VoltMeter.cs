using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class VoltMeter : BaseUI {
        protected const int FLAG_SHOWVOLTAGE = 1;

        protected PointF mCenter;
        PointF mPlusPoint;

        public VoltMeter(Point pos) : base(pos) {
            Elm = new ElmVoltMeter();
            /* default for new elements */
            mFlags = FLAG_SHOWVOLTAGE;
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
            Post.SetBbox(0);
            calcLeads(16);
            interpPost(ref mCenter, 0.5);
            interpPost(ref mPlusPoint, 8.0 / Post.Len, 6 * Post.Dsign);
        }

        public override void Draw(CustomGraphics g) {
            draw2Leads();

            if (this == CirSimForm.PlotXElm) {
                drawCenteredLText("X", mCenter, true);
            }
            if (this == CirSimForm.PlotYElm) {
                drawCenteredLText("Y", mCenter, true);
            }

            if (mustShowVoltage()) {
                drawCenteredText(drawValues(), mCenter);
            }

            drawCenteredLText("+", mPlusPoint, true);
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmVoltMeter)Elm;
            arr[0] = "電圧計";
            if (this is VoltMeter1Term) {
                arr[1] = "電位：" + drawValues();
            } else {
                arr[1] = "電位差：" + drawValues();
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmVoltMeter)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("表示", ce.Meter,
                    new string[] { "瞬時値", "実効値", "最大値", "最小値", "P-P" }
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

        protected string drawValues() {
            var ce = (ElmVoltMeter)Elm;
            switch (ce.Meter) {
            case ElmVoltMeter.TP_VOL:
                return Utils.UnitTextWithScale(ce.GetVoltageDiff(), "V", ce.Scale);
            case ElmVoltMeter.TP_RMS:
                return Utils.UnitTextWithScale(ce.RmsV, "Vrms", ce.Scale);
            case ElmVoltMeter.TP_MAX:
                return Utils.UnitTextWithScale(ce.LastMaxV, "Vpk", ce.Scale);
            case ElmVoltMeter.TP_MIN:
                return Utils.UnitTextWithScale(ce.LastMinV, "Vmin", ce.Scale);
            case ElmVoltMeter.TP_P2P:
                return Utils.UnitTextWithScale(ce.LastMaxV - ce.LastMinV, "Vp-p", ce.Scale);
            }
            return "";
        }

        protected bool mustShowVoltage() {
            return (mFlags & FLAG_SHOWVOLTAGE) != 0;
        }
    }
}
