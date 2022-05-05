﻿using System;

namespace Circuit.Elements.Input {
    class FMElmE : BaseElement {
        public double CarrierFreq;
        public double Signalfreq;
        public double MaxVoltage;
        public double Deviation;

        double mFreqTimeZero;
        double mLastTime = 0;
        double mFuncx = 0;

        public FMElmE() : base() {
            Deviation = 200;
            MaxVoltage = 5;
            CarrierFreq = 800;
            Signalfreq = 40;
            Reset();
        }

        public FMElmE(StringTokenizer st) : base() {
            CarrierFreq = st.nextTokenDouble();
            Signalfreq = st.nextTokenDouble();
            MaxVoltage = st.nextTokenDouble();
            Deviation = st.nextTokenDouble();
            Reset();
        }

        public override void Reset() {
            mFreqTimeZero = 0;
            CurCount = 0;
        }

        public override int PostCount { get { return 1; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override bool HasGroundConnection(int n1) { return true; }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        public override void DoStep() {
            mCir.UpdateVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
        }

        double getVoltage() {
            double deltaT = CirSim.Sim.Time - mLastTime;
            mLastTime = CirSim.Sim.Time;
            double signalamplitude = Math.Sin(2 * Math.PI * (CirSim.Sim.Time - mFreqTimeZero) * Signalfreq);
            mFuncx += deltaT * (CarrierFreq + (signalamplitude * Deviation));
            double w = 2 * Math.PI * mFuncx;
            return Math.Sin(w) * MaxVoltage;
        }
    }
}
