namespace MainForm.Forms;

public partial class EditDialog : Form {
	readonly CirSim cframe;
	readonly Editable elm;

	readonly EditInfo[] einfos = new EditInfo[10];
	readonly Panel vp, pnlBottons;
	readonly Button applyButton, okButton, cancelButton;

	int einfocount;
	const int barmax = 1000;
	bool closeOnEnter = true;
	const string noCommaFormat = "####.##########";
	const double ROOT2 = 1.41421356237309504880;

	public EditDialog(Editable ce, CirSim f) {
		InitializeComponent();

		Text = "Edit Component";
		cframe = f;
		elm = ce;

		vp = new Panel()
		{
			Left = 0,
			Top = 0,
			Width = Width,
			Height = Height,
			Anchor = AnchorStyles.Left | AnchorStyles.Top
		};
		Controls.Add(vp);

		pnlBottons = new Panel
		{
			Left = 0,
			Width = Width,
			Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
		};
		vp.Controls.Add(pnlBottons);

		applyButton = new Button
		{
			Text = "Apply"
		};
		applyButton.Click += (s, e) => {
			apply();
		};
		pnlBottons.Controls.Add(applyButton);

		okButton = new Button
		{
			Text = "OK"
		};
		okButton.Click += (s, e) => {
			apply();
			closeDialog();
		};
		pnlBottons.Controls.Add(okButton);

		cancelButton = new Button
		{
			Text = "Cancel"
		};
		cancelButton.Click += (s, e) => {
			closeDialog();
		};
		pnlBottons.Controls.Add(cancelButton);

		buildDialog();
		ShowDialog();
	}

	void buildDialog() {
		int i;
		for (i = 0; ; i++) {
			einfos[i] = elm.getEditInfo(i);
			if (einfos[i] == null) {
				break;
			}
			var ixLabel = vp.Controls.IndexOf(pnlBottons);
			var ei = einfos[i];
			var name = ei.name;
			Label? l = null;
			if (ei?.name?.IndexOf('<') != -1) {
				l = new LinkLabel() { Text = name };
				vp.Controls.SetChildIndex(l, ixLabel);
			} else {
				l = new Label() { Text = name };
				vp.Controls.SetChildIndex(l, ixLabel);
			}
			if (l != null && i != 0) {
				l.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
			}
			var ixCtrl = vp.Controls.IndexOf(pnlBottons);
			if (ei?.choice != null) {
				vp.Controls.SetChildIndex(ei.choice, ixCtrl);
				ei.choice.SelectedIndexChanged += (s, e) => {
					itemStateChanged(s);
				};
			} else if (ei?.checkbox != null) {
				vp.Controls.SetChildIndex(ei.checkbox, ixCtrl);
				ei.checkbox.CheckStateChanged += (s, e) => {
					itemStateChanged(s);
				};
			} else if (ei?.button != null) {
				vp.Controls.SetChildIndex(ei.button, ixCtrl);
				ei.button.Click += (s, e) => {
					itemStateChanged(s);
				};
			} else if (ei?.textArea != null) {
				vp.Controls.SetChildIndex(ei.textArea, ixCtrl);
				closeOnEnter = false;
			} else if (ei?.widget != null) {
				vp.Controls.SetChildIndex(ei.widget, ixCtrl);
			} else if (ei != null) {
				ei.textf = new TextBox();
				vp.Controls.SetChildIndex(ei.textf, ixCtrl);
				if (ei.text != null) {
					ei.textf.Text = ei.text;
				}
				if (ei.text == null) {
					ei.textf.Text = unitString(ei);
				}
			}
		}
		einfocount = i;
	}

	double diffFromInteger(double x) {
		return Math.Abs(x - Math.Round(x));
	}

	double parseUnits(EditInfo ei) {
		if (ei.textf == null) {
			throw new ArgumentException("EditInfo must have textf's text field to parse units");
		}
		var s = ei.textf.Text;
		return parseUnits(s);
	}

	static double parseUnits(string s) {
		s = s.Trim();
		double rmsMult = 1;
		if (s.LastIndexOf("rms") != -1) {
			s = s[..^3].Trim();
			rmsMult = ROOT2;
		}
		// TODO:using regex
		// rewrite shorthand (eg "2k2") in to normal format (eg 2.2k)
		s = s.Replace("([0-9]+)([pPnNuUmMkKgG])([0-9]+)", "$1.$3$2");
		// TODO:using regex
		// rewrite meg to M
		s = s.Replace("[mM][eE][gG]$", "M");
		var len = s.Length;
		var uc = s.ElementAt(len - 1);
		double mult = 1;
		switch (uc) {
		case 'f':
		case 'F':
			mult = 1e-15;
			break;
		case 'p':
		case 'P':
			mult = 1e-12;
			break;
		case 'n':
		case 'N':
			mult = 1e-9;
			break;
		case 'u':
		case 'U':
			mult = 1e-6;
			break;

		// for ohm values, we used to assume mega for lowercase m, otherwise milli
		case 'm':
			mult = /* (ei.forceLargeM) ? 1e6 : */ 1e-3;
			break;

		case 'k':
		case 'K':
			mult = 1e3;
			break;
		case 'M':
			mult = 1e6;
			break;
		case 'G':
		case 'g':
			mult = 1e9;
			break;
		}
		if (mult != 1) {
			s = s[..(len - 1)].Trim();
		}
		return double.Parse(s) * mult * rmsMult;
	}

	string unitString(EditInfo ei) {
		// for voltage elements, express values in rms if that would be shorter
		if (elm != null && elm is VoltageElm
			&& Math.Abs(ei.value) > 1e-4
			&& diffFromInteger(ei.value * 1e4) > diffFromInteger(ei.value * 1e4 / ROOT2)
		) {
			return unitString(ei, ei.value / ROOT2) + "rms";
		}
		return unitString(ei, ei.value);
	}

	static string unitString(EditInfo ei, double v) {
		var va = Math.Abs(v);
		if (ei != null && ei.dimensionless) {
			return v.ToString(noCommaFormat);
		}
		if (double.IsInfinity(va))
			return v.ToString(noCommaFormat);
		if (v == 0)
			return "0";
		if (va < 1e-12)
			return (v * 1e15).ToString(noCommaFormat) + "f";
		if (va < 1e-9)
			return (v * 1e12).ToString(noCommaFormat) + "p";
		if (va < 1e-6)
			return (v * 1e9).ToString(noCommaFormat) + "n";
		if (va < 1e-3)
			return (v * 1e6).ToString(noCommaFormat) + "u";
		if (va < 1 /* && !ei.forceLargeM */)
			return (v * 1e3).ToString(noCommaFormat) + "m";
		if (va < 1e3)
			return v.ToString(noCommaFormat);
		if (va < 1e6)
			return (v * 1e-3).ToString(noCommaFormat) + "k";
		if (va < 1e9)
			return (v * 1e-6).ToString(noCommaFormat) + "M";
		return (v * 1e-9).ToString(noCommaFormat) + "G";
	}

	void apply() {
		int i;
		for (i = 0; i != einfocount; i++) {
			var ei = einfos[i];
			if (ei.textf != null && ei.text == null) {
				try {
					ei.value = parseUnits(ei);
				} catch {
					/* ignored */
				}
			}
			if (ei.button != null) {
				continue;
			}
			elm.setEditValue(i, ei);

			// update slider if any
			if (elm is ElmBase) {
				var adj = cframe.findAdjustable((ElmBase) elm, i);
				if (adj != null) {
					/// TODO:TrackBar
					adj.Value = (int)ei.value;
				}
			}
		}
		cframe.needAnalyze();
	}

	protected void closeDialog() {
		Visible = false;
		if (cframe.editDialog == this) {
			cframe.editDialog = null;
		}
	}

	public void enterPressed() {
		if (closeOnEnter) {
			apply();
			closeDialog();
		}
	}

	public void itemStateChanged(object? src) {
		if (src != null) {
			return;
		}
		var changed = false;
		var applied = false;
		for (int i = 0; i != einfocount; i++) {
			var ei = einfos[i];
			if (ei.choice == src || ei.checkbox == src || ei.button == src) {
				// if we're pressing a button, make sure to apply changes first
				if (ei.button == src && !ei.newDialog) {
					apply();
					applied = true;
				}
				elm.setEditValue(i, ei);
				if (ei.newDialog) {
					changed = true;
				}
				cframe.needAnalyze();
			}
		}
		if (changed) {
			// apply changes before we reset everything
			// (need to check if we already applied changes; otherwise Diode create simple
			// model button doesn't work)
			if (!applied) {
				apply();
			}
			clearDialog();
			buildDialog();
		}
	}

	public void resetDialog() {
		clearDialog();
		buildDialog();
	}

	public void clearDialog() {
		while (vp.Controls[0] != pnlBottons) {
			vp.Controls.RemoveAt(0);
		}
	}
}
