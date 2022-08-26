using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class InductorUI : BaseUI {
        protected static string mLastReferenceName = "L";

        const int BODY_LEN = 24;

        public InductorUI(Point pos) : base(pos) {
            Elm = new InductorElm();
            DumpInfo.ReferenceName = mLastReferenceName;
        }

        public InductorUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                Elm = new InductorElm(st);
                DumpInfo.ReferenceName = st.nextToken();
            } catch (Exception ex) {
                throw new Exception("Inductor load error:{0}", ex);
            }
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.INDUCTOR; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INDUCTOR; } }

        protected override void dump(List<object> optionList) {
            var ce = (InductorElm)Elm;
            optionList.Add(ce.Inductance);
            optionList.Add(ce.Current.ToString("0.000000"));
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            setTextPos();
        }

        void setTextPos() {
            mNameV = mPost1.X == mPost2.X;
            mNameH = mPost1.Y == mPost2.Y;
            if (mNameH) {
                interpPoint(ref mValuePos, 0.5, -11 * mDsign);
                interpPoint(ref mNamePos, 0.5, 10 * mDsign);
            } else if (mNameV) {
                interpPoint(ref mValuePos, 0.5, mDsign);
                interpPoint(ref mNamePos, 0.5, -20 * mDsign);
            } else {
                interpPoint(ref mValuePos, 0.5, 8 * mDsign);
                interpPoint(ref mNamePos, 0.5, -8 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (InductorElm)Elm;
            double v1 = Elm.Volts[0];
            double v2 = Elm.Volts[1];
            int hs = 8;
            setBbox(mPost1, mPost2, hs);

            draw2Leads();
            drawCoil(mLead1, mLead2, v1, v2);

            drawValue(ce.Inductance);
            drawName();

            doDots();
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (InductorElm)Elm;
            arr[0] = string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "コイル" : DumpInfo.ReferenceName;
            getBasicInfo(arr);
            arr[3] = "L = " + Utils.UnitText(ce.Inductance, "H");
            arr[4] = "P = " + Utils.UnitText(ce.Power, "W");
        }

        public override string GetScopeText() {
            var ce = (InductorElm)Elm;
            return (string.IsNullOrEmpty(DumpInfo.ReferenceName) ? "コイル" : DumpInfo.ReferenceName) + " "
                + Utils.UnitText(ce.Inductance, "H");
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (InductorElm)Elm;
            if (n == 0) {
                return new ElementInfo("インダクタンス(H)", ce.Inductance, 0, 0);
            }
            if (n == 1) {
                var ei = new ElementInfo("名前", 0, 0, 0);
                ei.Text = DumpInfo.ReferenceName;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (InductorElm)Elm;
            if (n == 0 && ei.Value > 0) {
                ce.Inductance = ei.Value;
                setTextPos();
            }
            if (n == 1) {
                DumpInfo.ReferenceName = ei.Textf.Text;
                mLastReferenceName = DumpInfo.ReferenceName;
                setTextPos();
            }
            ce.Setup(ce.Inductance, Elm.Current);
        }

        public override EventHandler CreateSlider(ElementInfo ei, Adjustable adj) {
            var ce = (InductorElm)Elm;
            return new EventHandler((s, e) => {
                var trb = adj.Slider;
                ce.Inductance = adj.MinValue + (adj.MaxValue - adj.MinValue) * trb.Value / trb.Maximum;
                CirSimForm.Sim.NeedAnalyze();
            });
        }
    }
}
