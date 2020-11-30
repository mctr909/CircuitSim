
namespace Circuit.Elements {
    class ClockElm : RailElm {
        public ClockElm(int xx, int yy) : base(xx, yy, WF_SQUARE) {
            maxVoltage = 2.5;
            bias = 2.5;
            frequency = 100;
            mFlags |= FLAG_CLOCK;
        }
    }
}
