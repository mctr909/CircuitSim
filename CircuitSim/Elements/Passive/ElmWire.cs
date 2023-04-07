﻿namespace Circuit.Elements.Passive {
    class ElmWire : BaseElement {
        public bool HasWireInfo; /* used in CirSim to calculate wire currents */

        public override int PostCount { get { return 2; } }

        public override bool IsWire { get { return true; } }

        public override double GetVoltageDiff() { return Volts[0]; }
    }
}
