using System.Drawing;

namespace Circuit.Elements.Input {
    class RailUIClock : RailUI {
        public RailUIClock(Point pos) : base(pos, VoltageElm.WAVEFORM.SQUARE) {
            var elm = (VoltageElm)CirElm;
            elm.mMaxVoltage = 2.5;
            elm.mBias = 2.5;
            elm.mFrequency = 100;
            mFlags |= FLAG_CLOCK;
        }
    }
}
