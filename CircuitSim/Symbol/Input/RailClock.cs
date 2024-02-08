using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
    class RailClock : Rail {
        public RailClock(Point pos) : base(pos, ElmVoltage.WAVEFORM.SQUARE) {
            var elm = (ElmVoltage)Elm;
            elm.MaxVoltage = 2.5;
            elm.Bias = 2.5;
            elm.Frequency = 100;
            mFlags |= FLAG_CLOCK;
        }
    }
}
