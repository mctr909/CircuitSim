using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.Symbol.Output {
    class VoltMeter : BaseSymbol {
        protected const int FLAG_SHOWVOLTAGE = 1;

        protected PointF mCenter;
        PointF mPlusPoint;
        ElmVoltMeter mElm;

        public override BaseElement Element { get { return mElm; } }

        public VoltMeter(Point pos) : base(pos) {
            mElm = new ElmVoltMeter();
            /* default for new elements */
            mFlags = FLAG_SHOWVOLTAGE;
        }

        public VoltMeter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mElm = new ElmVoltMeter(st);
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.VOLTMETER; } }

        protected override void dump(List<object> optionList) {
            optionList.Add(mElm.Meter);
            optionList.Add(mElm.Scale);
        }

        public override void SetPoints() {
            base.SetPoints();
            SetLeads(16);
            InterpolationPost(ref mCenter, 0.5);
            InterpolationPost(ref mPlusPoint, 8.0 / Post.Len, 6 * Post.Dsign);
        }

        public override void Draw(CustomGraphics g) {
            Draw2Leads();

            if (this == CirSimForm.PlotXElm) {
                DrawCenteredLText("X", mCenter);
            }
            if (this == CirSimForm.PlotYElm) {
                DrawCenteredLText("Y", mCenter);
            }

            if (MustShowVoltage()) {
                DrawCenteredText(DrawValues(), mCenter);
            }

            DrawCenteredLText("+", mPlusPoint);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "電圧計";
            if (this is VoltMeter1Term) {
                arr[1] = "電位：" + DrawValues();
            } else {
                arr[1] = "電位差：" + DrawValues();
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("表示", mElm.Meter,
                    new string[] { "瞬時値", "実効値", "最大値", "最小値", "P-P" }
                );
            }
            if (r == 1) {
                return new ElementInfo("スケール", (int)mElm.Scale, new string[] { "自動", "V", "mV", "uV" });
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            if (n == 0) {
                mElm.Meter = ei.Choice.SelectedIndex;
            }
            if (n == 1) {
                mElm.Scale = (E_SCALE)ei.Choice.SelectedIndex;
            }
        }

        protected string DrawValues() {
            switch (mElm.Meter) {
            case ElmVoltMeter.TP_VOL:
                return Utils.UnitTextWithScale(mElm.VoltageDiff, "V", mElm.Scale);
            case ElmVoltMeter.TP_RMS:
                return Utils.UnitTextWithScale(mElm.RmsV, "Vrms", mElm.Scale);
            case ElmVoltMeter.TP_MAX:
                return Utils.UnitTextWithScale(mElm.LastMaxV, "Vpk", mElm.Scale);
            case ElmVoltMeter.TP_MIN:
                return Utils.UnitTextWithScale(mElm.LastMinV, "Vmin", mElm.Scale);
            case ElmVoltMeter.TP_P2P:
                return Utils.UnitTextWithScale(mElm.LastMaxV - mElm.LastMinV, "Vp-p", mElm.Scale);
            }
            return "";
        }

        protected bool MustShowVoltage() {
            return (mFlags & FLAG_SHOWVOLTAGE) != 0;
        }
    }
}
