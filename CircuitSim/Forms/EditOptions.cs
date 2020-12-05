using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    class EditOptions : Editable {
        CirSim sim;

        public EditOptions(CirSim s) { sim = s; }

        public EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Time step size (s)", sim.timeStep, 0, 0);
            }
            if (n == 1) {
                return new EditInfo("Range for voltage color (V)", CircuitElm.VoltageRange, 0, 0);
            }
            if (n == 2) {
                var ei = new EditInfo("Change Language", 0, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("(no change)");
                return ei;
            }

            return null;
        }

        public void SetEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.Value > 0) {
                sim.timeStep = ei.Value;
                /* if timestep changed manually, prompt before changing it again */
                // TODO: setEditValue
                //AudioOutputElm.okToChangeTimeStep = false;
            }
            if (n == 1 && ei.Value > 0) {
                CircuitElm.VoltageRange = ei.Value;
            }
            if (n == 2) {
                int lang = ei.Choice.SelectedIndex;
                if (lang == 0) {
                    return;
                }
                string langString = null;
                switch (lang) {
                case 1: langString = "da"; break;
                case 2: langString = "de"; break;
                case 3: langString = "en"; break;
                case 4: langString = "es"; break;
                case 5: langString = "fr"; break;
                case 6: langString = "it"; break;
                case 7: langString = "pl"; break;
                case 8: langString = "pt"; break;
                case 9: langString = "ru"; break;
                }
                if (langString == null) {
                    return;
                }
                var stor = Storage.getLocalStorageIfSupported();
                if (stor == null) {
                    MessageBox.Show("Can't set language");
                    return;
                }
                stor.setItem("language", langString);
                /*if (MessageBox.Show("Must restart to set language.  Restart now?") == DialogResult.OK) {
                    Window.Location.reload();
                }*/
            }
        }
    }
}
