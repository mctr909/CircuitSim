using System.Drawing;

namespace Circuit.InputElements {
    class ClockElm : RailElm {
        public ClockElm(Point pos) : base(pos, WAVEFORM.SQUARE) {
            maxVoltage = 2.5;
            bias = 2.5;
            frequency = 100;
            mFlags |= FLAG_CLOCK;
        }
    }
}
