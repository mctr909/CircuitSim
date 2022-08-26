using System.Drawing;

namespace Circuit.Elements.Input {
    class RailUIClock : RailUI {
        public RailUIClock(Point pos) : base(pos, VoltageElm.WAVEFORM.SQUARE) {
            var elm = (VoltageElm)Elm;
            elm.MaxVoltage = 2.5;
            elm.Bias = 2.5;
            elm.Frequency = 100;
            DumpInfo.Flags |= FLAG_CLOCK;
        }
    }
}
