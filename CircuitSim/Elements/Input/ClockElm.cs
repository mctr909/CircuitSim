using System.Drawing;

namespace Circuit.Elements.Input {
    class ClockElm : RailElm {
        public ClockElm(Point pos) : base(pos, VoltageElmE.WAVEFORM.SQUARE) {
            var elm = (VoltageElmE)CirElm;
            elm.mMaxVoltage = 2.5;
            elm.mBias = 2.5;
            elm.mFrequency = 100;
            mFlags |= FLAG_CLOCK;
        }
    }
}
