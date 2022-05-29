using System;
using System.Windows.Forms;

namespace Circuit {
    public partial class CircuitSim : Form {
        CirSimForm sim;

        public CircuitSim() {
            InitializeComponent();
            sim = new CirSimForm(this);
        }

        private void Form1_Load(object sender, EventArgs e) {
            Width = 800;
            Height = 600;
        }
    }
}
