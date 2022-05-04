using System;

namespace Circuit.Elements.Active {
    class DiodeElmE : BaseElement {
        public static string lastModelName = "default";

        public string mModelName;
        public DiodeModel mModel;

        Diode mDiode;
        bool mHasResistance;
        int mDiodeEndNode;

        public DiodeElmE() : base() {
            mModelName = lastModelName;
            mDiode = new Diode(mCir);
            Setup();
        }

        public DiodeElmE(StringTokenizer st, bool forwardDrop = false, bool model = false) : base() {
            const double defaultdrop = 0.805904783;
            mDiode = new Diode(mCir);
            double fwdrop = defaultdrop;
            double zvoltage = 0;
            if (model) {
                try {
                    mModelName = CustomLogicModel.unescape(st.nextToken());
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

        public override int CirInternalNodeCount { get { return mHasResistance ? 1 : 0; } }

        public override bool CirNonLinear { get { return true; } }

        public override void CirStamp() {
            if (mHasResistance) {
                /* create diode from node 0 to internal node */
                mDiode.Stamp(CirNodes[0], CirNodes[2]);
                /* create resistor from internal node to node 1 */
                mCir.StampResistor(CirNodes[1], CirNodes[2], mModel.SeriesResistance);
            } else {
                /* don't need any internal nodes if no series resistance */
                mDiode.Stamp(CirNodes[0], CirNodes[1]);
            }
        }

        public override void CirDoStep() {
            mDiode.DoStep(CirVolts[0] - CirVolts[mDiodeEndNode]);
        }

        public override void CirStepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(mCirCurrent) > 1e12) {
                mCir.Stop("max current exceeded", this);
            }
        }

        public override void CirReset() {
            mDiode.Reset();
            CirVolts[0] = CirVolts[1] = mCirCurCount = 0;
            if (mHasResistance) {
                CirVolts[2] = 0;
            }
        }

        protected override void cirCalculateCurrent() {
            mCirCurrent = mDiode.CalculateCurrent(CirVolts[0] - CirVolts[mDiodeEndNode]);
        }

        public void Setup() {
            mModel = DiodeModel.GetModelWithNameOrCopy(mModelName, mModel);
            mModelName = mModel.Name;
            mDiode.Setup(mModel);
            mHasResistance = 0 < mModel.SeriesResistance;
            mDiodeEndNode = mHasResistance ? 2 : 1;
            cirAllocNodes();
        }
    }
}
