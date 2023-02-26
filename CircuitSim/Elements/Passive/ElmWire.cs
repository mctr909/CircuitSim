﻿namespace Circuit.Elements.Passive {
    class ElmWire : BaseElement {
        public bool HasWireInfo; /* used in CirSim to calculate wire currents */

        public override int PostCount { get { return 2; } }

        public override bool IsWire { get { return true; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return 0; } }
    }
}
