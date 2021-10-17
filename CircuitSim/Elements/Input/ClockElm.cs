using System.Drawing;

namespace Circuit.Elements.Input {
    class ClockElm : RailElm {
        public ClockElm(Point pos) : base(pos, WAVEFORM.SQUARE) {
            mMaxVoltage = 2.5;
            mBias = 2.5;
            mFrequency = 100;
            mFlags |= FLAG_CLOCK;
        }
    }
}
