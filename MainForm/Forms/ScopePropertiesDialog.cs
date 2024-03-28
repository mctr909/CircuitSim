namespace MainForm.Forms {
	public partial class ScopePropertiesDialog : Form {
		public ScopePropertiesDialog(CirSim asim, Scope s) {
			InitializeComponent();
		}

		public static double nextHighestScale(double d) {
			d = d * 1.001; // Go just above last check point
			double s;
			s = Scope.MIN_MAN_SCALE;
			for (int a = 0; s < d; a++) { // Iterate until we go over the target
				s *= Scope.multa[a % 3];
			}
			return s;
		}
	}
}
