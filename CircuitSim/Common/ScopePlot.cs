﻿using System.Drawing;

using Circuit.Elements;

namespace Circuit {
    class ScopePlot {
        static readonly Color[] COLORS = {
            Color.FromArgb(0xFF, 0x00, 0x00),
            Color.FromArgb(0xFF, 0x80, 0x00),
            Color.FromArgb(0xFF, 0x00, 0xFF),
            Color.FromArgb(0x7F, 0x00, 0xFF),
            Color.FromArgb(0x00, 0x00, 0xFF),
            Color.FromArgb(0x00, 0x80, 0xFF),
            Color.FromArgb(0xFF, 0xFF, 0x00),
            Color.FromArgb(0x00, 0xFF, 0xFF)
        };

        public CircuitElm Elm;

        public double[] MinValues { get; private set; }
        public double[] MaxValues { get; private set; }
        public int Pointer { get; private set; }
        public int Value { get; private set; }
        public int Speed { get; private set; }
        public int Units { get; set; }
        public double LastValue { get; private set; }
        public Color Color { get; private set; }

        int mScopePointCount;
        int mCounter;

        public ScopePlot(CircuitElm e, int u) {
            Elm = e;
            Units = u;
        }

        public ScopePlot(CircuitElm e, int u, int v) {
            Elm = e;
            Units = u;
            Value = v;
        }

        public int StartIndex(int w) {
            return Pointer + mScopePointCount - w;
        }

        public void Reset(int spc, int sp, bool full) {
            int oldSpc = mScopePointCount;
            mScopePointCount = spc;
            if (Speed != sp) {
                oldSpc = 0; /* throw away old data */
            }
            Speed = sp;
            var oldMin = MinValues;
            var oldMax = MaxValues;
            MinValues = new double[mScopePointCount];
            MaxValues = new double[mScopePointCount];
            if (oldMin != null && !full) {
                /* preserve old data if possible */
                int i;
                for (i = 0; i != mScopePointCount && i != oldSpc; i++) {
                    int i1 = (-i) & (mScopePointCount - 1);
                    int i2 = (Pointer - i) & (oldSpc - 1);
                    MinValues[i1] = oldMin[i2];
                    MaxValues[i1] = oldMax[i2];
                }
            } else {
                mCounter = 0;
            }
            Pointer = 0;
        }

        public void TimeStep() {
            if (Elm == null) {
                return;
            }
            double v = Elm.GetScopeValue(Value);
            if (v < MinValues[Pointer]) {
                MinValues[Pointer] = v;
            }
            if (v > MaxValues[Pointer]) {
                MaxValues[Pointer] = v;
            }
            LastValue = v;
            mCounter++;
            if (mCounter >= Speed) {
                Pointer = (Pointer + 1) & (mScopePointCount - 1);
                MinValues[Pointer] = MaxValues[Pointer] = v;
                mCounter = 0;
            }
        }

        public string GetUnitText(double v) {
            switch (Units) {
            case Scope.UNITS_V:
                return CircuitElm.getVoltageText(v);
            case Scope.UNITS_A:
                return CircuitElm.getCurrentText(v);
            case Scope.UNITS_OHMS:
                return CircuitElm.getUnitText(v, CirSim.ohmString);
            case Scope.UNITS_W:
                return CircuitElm.getUnitText(v, "W");
            }
            return null;
        }

        public void AssignColor(int count) {
            if (count > 0) {
                Color = COLORS[(count - 1) % 8];
                return;
            }
            switch (Units) {
            case Scope.UNITS_V:
                Color = Color.FromArgb(0x00, 0xFF, 0x00);
                break;
            case Scope.UNITS_A:
                Color = Color.FromArgb(0xFF, 0xFF, 0x00);
                break;
            default:
                Color = CirSim.theSim.chkPrintableCheckItem.Checked ? Color.FromArgb(0, 0, 0) : Color.FromArgb(255, 255, 255);
                break;
            }
        }
    }
}
