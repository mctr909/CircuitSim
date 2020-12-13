using Circuit.Elements;

namespace Circuit {
    class EditOptions : Editable {
        CirSim sim;

        public EditOptions(CirSim s) { sim = s; }

        public EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Time step size (s)", sim.timeStep, 0, 0);
            }
            if (n == 1) {
                return new EditInfo("Range for voltage color (V)", CircuitElm.VoltageRange, 0, 0);
            }
            return null;
        }

        public void SetEditValue(int n, EditInfo ei) {
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
