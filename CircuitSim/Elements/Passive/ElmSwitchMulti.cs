using System.Drawing;

namespace Circuit.Elements.Passive {
    class ElmSwitchMulti : ElmSwitch {
        public int ThrowCount = 2;
        public Point[] SwPosts;

        public override bool IsWire { get { return true; } }

        public override int AnaVoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1 + ThrowCount; } }

        public override Point GetPost(int n) {
            return (n == 0) ? Post[0] : SwPosts[n - 1];
        }

        public override bool AnaGetConnection(int n1, int n2) {
            return ComparePair(n1, n2, 0, 1 + Position);
        }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(Nodes[0], Nodes[Position + 1], mVoltSource, 0);
        }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 0) {
                return -Current;
            }
            if (n == Position + 1) {
                return Current;
            }
            return 0;
        }

    }
}
