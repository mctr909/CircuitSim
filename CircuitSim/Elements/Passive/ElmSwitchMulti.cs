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
            var n0 = Nodes[0] - 1;
            var n1 = Nodes[Position + 1] - 1;
            int vn = Circuit.Nodes.Count + mVoltSource - 1;
            Circuit.Matrix[vn, n0] -= 1;
            Circuit.Matrix[vn, n1] += 1;
            Circuit.Matrix[n0, vn] += 1;
            Circuit.Matrix[n1, vn] -= 1;
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
