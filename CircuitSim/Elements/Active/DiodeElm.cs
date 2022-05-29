using System;

namespace Circuit.Elements.Active {
    class DiodeElm : BaseElement {
        public static string lastModelName = "default";

        public string mModelName;
        public DiodeModel mModel;

        Diode mDiode;
        bool mHasResistance;
        int mDiodeEndNode;

        public DiodeElm() : base() {
            mModelName = lastModelName;
            mDiode = new Diode();
            Setup();
        }

        public DiodeElm(StringTokenizer st, bool forwardDrop = false, bool model = false) : base() {
            const double defaultdrop = 0.805904783;
            mDiode = new Diode();
            double fwdrop = defaultdrop;
            double zvoltage = 0;
            if (model) {
                try {
                    mModelName = Utils.Unescape(st.nextToken());
                } catch { }
            } else {
                if (forwardDrop) {
                    try {
                        fwdrop = st.nextTokenDouble();
                    } catch { }
                }
                mModel = DiodeModel.GetModelWithParameters(fwdrop, zvoltage);
                mModelName = mModel.Name;
            }
            Setup();
        }

        public override int PostCount { get { return 2; } }

        public override int InternalNodeCount { get { return mHasResistance ? 1 : 0; } }

        public override bool NonLinear { get { return true; } }

        public void Setup() {
            mModel = DiodeModel.GetModelWithNameOrCopy(mModelName, mModel);
            mModelName = mModel.Name;
            mDiode.Setup(mModel);
            mHasResistance = 0 < mModel.SeriesResistance;
            mDiodeEndNode = mHasResistance ? 2 : 1;
            AllocNodes();
        }

        public override void Reset() {
            mDiode.Reset();
            Volts[0] = Volts[1] = CurCount = 0;
            if (mHasResistance) {
                Volts[2] = 0;
            }
        }

        public override void AnaStamp() {
            if (mHasResistance) {
                /* create diode from node 0 to internal node */
                mDiode.Stamp(Nodes[0], Nodes[2]);
                /* create resistor from internal node to node 1 */
                Circuit.StampResistor(Nodes[1], Nodes[2], mModel.SeriesResistance);
            } else {
                /* don't need any internal nodes if no series resistance */
                mDiode.Stamp(Nodes[0], Nodes[1]);
            }
        }

        public override void CirDoStep() {
            mDiode.CirDoStep(Volts[0] - Volts[mDiodeEndNode]);
        }

        public override void CirSetNodeVoltage(int n, double c) {
            Volts[n] = c;
            mCurrent = mDiode.CirCalculateCurrent(Volts[0] - Volts[mDiodeEndNode]);
        }

        public override void CirStepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(mCurrent) > 1e12) {
                Circuit.Stop("max current exceeded", this);
            }
        }
    }
}
