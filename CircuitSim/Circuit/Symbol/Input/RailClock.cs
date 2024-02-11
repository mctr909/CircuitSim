using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
    class RailClock : Rail {
        public RailClock(Point pos) : base(pos, ElmVoltage.WAVEFORM.SQUARE) {
            mElm.MaxVoltage = 2.5;
            mElm.Bias = 2.5;
            mElm.Frequency = 100;
            mFlags |= FLAG_CLOCK;
        }
    }
}
