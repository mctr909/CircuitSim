﻿namespace Circuit {
    class Inductor {
        int[] nodes;
        Circuit cir;

        double inductance;
        double compResistance;
        double current;
        double curSourceValue;

        public virtual bool NonLinear() { return false; }

        public Inductor(Circuit c) {
            cir = c;
            nodes = new int[2];
        }

        public void Setup(double ic, double cr) {
            inductance = ic;
            current = cr;
        }

        public void Reset() {
            /* need to set curSourceValue here in case one of inductor nodes is node 0.  In that case
             * calculateCurrent() may get called (from setNodeVoltage()) when analyzing circuit, before
             * startIteration() gets called */
            current = curSourceValue = 0;
        }

        public void Stamp(int n0, int n1) {
            /* inductor companion model using trapezoidal or backward euler
             * approximations (Norton equivalent) consists of a current
             * source in parallel with a resistor.  Trapezoidal is more
             * accurate than backward euler but can cause oscillatory behavior.
             * The oscillation is a real problem in circuits with switches. */
            nodes[0] = n0;
            nodes[1] = n1;
            compResistance = 2 * inductance / ControlPanel.TimeStep;
            cir.StampResistor(nodes[0], nodes[1], compResistance);
            cir.StampRightSide(nodes[0]);
            cir.StampRightSide(nodes[1]);
        }

        public void StartIteration(double voltdiff) {
            curSourceValue = voltdiff / compResistance + current;
        }

        public double CalculateCurrent(double voltdiff) {
            /* we check compResistance because this might get called
             * before stamp(), which sets compResistance, causing
             * infinite current */
            if (compResistance > 0) {
                current = voltdiff / compResistance + curSourceValue;
            }
            return current;
        }

        public void DoStep(double voltdiff) {
            cir.StampCurrentSource(nodes[0], nodes[1], curSourceValue);
        }
    }
}
