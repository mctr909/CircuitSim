namespace Circuit.Elements.Passive {
	class ElmSwitch : BaseElement {
		public int ThrowCount = 2;
		public int Position = 0;

		public override int TermCount { get { return 1 + ThrowCount; } }

		protected override double GetCurrent(int n) {
			if (n == 0) {
				return -I[0];
			}
			if (n == Position + 1) {
				return I[0];
			}
			return 0;
		}
	}
}
