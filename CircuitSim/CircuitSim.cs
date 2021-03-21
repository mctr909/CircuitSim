using System;
using System.Windows.Forms;

namespace Circuit {
    public partial class CircuitSim : Form {
        CirSim sim;

        public CircuitSim() {
            InitializeComponent();
            sim = new CirSim();
        }

        private void Form1_Load(object sender, EventArgs e) {
            sim.Init(this);
            Width = 800;
            Height = 600;
        }
    }
}
