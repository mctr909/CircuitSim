﻿using System;

namespace Circuit.Elements.Input {
    class ElmAM : BaseElement {
        public double CarrierFreq;
        public double SignalFreq;
        public double Depth;
        public double MaxVoltage;
        public double Phase;

        double mFreqTimeZero;

        public ElmAM() : base() {
            MaxVoltage = 5;
            CarrierFreq = 1000;
            SignalFreq = 40;
            Depth = 0.1;
            Phase = 0.0;
            Reset();
        }

        public ElmAM(StringTokenizer st) : base() {
            try {
                CarrierFreq = st.nextTokenDouble();
                SignalFreq = st.nextTokenDouble();
                MaxVoltage = st.nextTokenDouble();
                Phase = st.nextTokenDouble();
                Depth = st.nextTokenDouble();
            } catch { }
            Reset();
        }

        public override int PostCount { get { return 1; } }

        public override int AnaVoltageSourceCount { get { return 1; } }

        public override double GetVoltageDiff() { return Volts[0]; }

        public override double GetPower() { return -GetVoltageDiff() * Current; }

        public override void Reset() {
            mFreqTimeZero = 0;
        }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        public override void CirDoIteration() {
            var vn = Circuit.Nodes.Count + mVoltSource;
            var row = Circuit.RowInfo[vn - 1].MapRow;
            var th = 2 * Math.PI * (Circuit.Time - mFreqTimeZero);
            Circuit.RightSide[row] += (Math.Sin(th * SignalFreq + Phase) * Depth + 2 - Depth) / 2 * Math.Sin(th * CarrierFreq) * MaxVoltage;
        }
    }
}
