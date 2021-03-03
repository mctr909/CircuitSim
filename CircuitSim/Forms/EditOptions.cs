using Circuit.Elements;

namespace Circuit {
    class EditOptions : Editable {
        CirSim sim;

        public EditOptions(CirSim s) { sim = s; }

        public ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Time step size (s)", sim.timeStep, 0, 0);
            }
            if (n == 1) {
                return new ElementInfo("Range for voltage color (V)", CircuitElm.VoltageRange, 0, 0);
            }
            return null;
        }

        public void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value > 0) {
                sim.timeStep = ei.Value;
                /* if timestep changed manually, prompt before changing it again */
                // TODO: setEditValue
                //AudioOutputElm.okToChangeTimeStep = false;
            }
            if (n == 1 && ei.Value > 0) {
                CircuitElm.VoltageRange = ei.Value;
            }
        }
    }
}
