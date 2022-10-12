﻿using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class RailNoise : Rail {
        public RailNoise(Point pos) : base(pos, ElmVoltage.WAVEFORM.NOISE) { }
    }
}
