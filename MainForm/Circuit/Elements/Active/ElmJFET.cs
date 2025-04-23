namespace Circuit.Elements.Active {
	class ElmJFET : ElmFET {
		#region [method(Circuit)]
		protected override void DoIteration() {
			base.DoIteration();
			DiodeDoIteration(Para[N_CH] * (V[G] - V[S]), ref V[VD_D1], Nodes[D1_A], Nodes[D1_B]);
		}

		protected override double GetCurrent(int n) {
			if (n == 0) {
				return -I[CUR_D1];
			}
			if (n == 1) {
				return I[0] + I[CUR_D1];
			}
			return -I[0];
		}

		protected override void SetCurrent(int n, double i) {
			I[CUR_D1] = Para[N_CH] * DiodeCalculateCurrent(Para[N_CH] * (V[G] - V[S]));
		}
		#endregion
	}
}
