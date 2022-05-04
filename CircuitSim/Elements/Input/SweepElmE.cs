using System;

namespace Circuit.Elements.Input {
    class SweepElmE : BaseElement {
        public double MaxV;
        public double MaxF;
        public double MinF;
        public double SweepTime;
        public bool IsLog;
        public bool BothSides;

        public double Frequency { get; private set; }

        double mFadd;
        double mFmul;
        double mFreqTime;
        double mSavedTimeStep;
        double mVolt;
        int mFdir = 1;

        public SweepElmE() : base() {
            MinF = 20;
            MaxF = 4000;
            MaxV = 5;
            SweepTime = 0.1;
            CirReset();
        }

        public SweepElmE(StringTokenizer st) : base() {
            MinF = st.nextTokenDouble();
            MaxF = st.nextTokenDouble();
            MaxV = st.nextTokenDouble();
            SweepTime = st.nextTokenDouble();
            CirReset();
        }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override double CirPower { get { return -CirVoltageDiff * mCirCurrent; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override int CirPostCount { get { return 1; } }

        public override bool CirHasGroundConnection(int n1) { return true; }

        public override void CirStartIteration() {
            /* has timestep been changed? */
            if (ControlPanel.TimeStep != mSavedTimeStep) {
                setParams();
            }
            mVolt = Math.Sin(mFreqTime) * MaxV;
            mFreqTime += Frequency * 2 * Math.PI * ControlPanel.TimeStep;
            Frequency = Frequency * mFmul + mFadd;
            if (Frequency >= MaxF && mFdir == 1) {
                if (BothSides) {
                    mFadd = -mFadd;
                    mFmul = 1 / mFmul;
                    mFdir = -1;
                } else {
                    Frequency = MinF;
                }
            }
            if (Frequency <= MinF && mFdir == -1) {
                mFadd = -mFadd;
                mFmul = 1 / mFmul;
                mFdir = 1;
            }
        }

        public override void CirDoStep() {
            mCir.UpdateVoltageSource(0, CirNodes[0], mCirVoltSource, mVolt);
        }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[0], mCirVoltSource);
        }

        public override void CirReset() {
            Frequency = MinF;
            mFreqTime = 0;
            mFdir = 1;
            setParams();
        }

        public void setParams() {
            if (Frequency < MinF || Frequency > MaxF) {
                Frequency = MinF;
                mFreqTime = 0;
                mFdir = 1;
            }
            if (IsLog) {
                mFadd = 0;
                mFmul = Math.Pow(MaxF / MinF, mFdir * ControlPanel.TimeStep / SweepTime);
            } else {
                mFadd = mFdir * ControlPanel.TimeStep * (MaxF - MinF) / SweepTime;
                mFmul = 1;
            }
            mSavedTimeStep = ControlPanel.TimeStep;
        }
    }
}
