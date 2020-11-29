using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    class EditOptions : Editable {
        CirSim sim;

        public EditOptions(CirSim s) { sim = s; }

        public EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Time step size (s)", sim.timeStep, 0, 0);
            }
            if (n == 1) {
                return new EditInfo("Range for voltage color (V)", CircuitElm.voltageRange, 0, 0);
            }
            if (n == 2) {
                var ei = new EditInfo("Change Language", 0, -1, -1);
                ei.choice = new ComboBox();
                ei.choice.Items.Add("(no change)");
                //ei.choice.Items.Add("Dansk");
                //ei.choice.Items.Add("Deutsch");
                //ei.choice.Items.Add("English");
                //ei.choice.Items.Add("Español");
                //ei.choice.Items.Add("Français");
                //ei.choice.Items.Add("Italiano");
                //ei.choice.Items.Add("Polski");
                //ei.choice.Items.Add("Português");
                //ei.choice.Items.Add("\u0420\u0443\u0441\u0441\u043a\u0438\u0439"); // Russian 
                return ei;
            }

            return null;
        }

        public void setEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.value > 0) {
                sim.timeStep = ei.value;
                /* if timestep changed manually, prompt before changing it again */
                // TODO: setEditValue
                //AudioOutputElm.okToChangeTimeStep = false;
            }
            if (n == 1 && ei.value > 0) {
                CircuitElm.voltageRange = ei.value;
            }
            if (n == 2) {
                int lang = ei.choice.SelectedIndex;
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
