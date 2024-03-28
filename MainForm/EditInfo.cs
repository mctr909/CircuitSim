namespace MainForm;

public class EditInfo {
	public string? name;
	public string? text;
	public double value;

	public bool newDialog;
	public bool dimensionless;
	public bool noSliders;

	public TextBox? textf;
	public ComboBox? choice;
	public CheckBox? checkbox;
	public Button? button;
	public TextBox? textArea;
	public Control? widget;

	// for slider dialog
	public TextBox? labelBox;
	public TextBox? minBox, maxBox;

	public EditInfo() { }

	public EditInfo(string n, double val, double mn, double mx) {
		name = n;
		value = val;
		dimensionless = false;
	}

	public EditInfo(string n, double val) {
		name = n;
		value = val;
		dimensionless = false;
	}

	public EditInfo(string n, string txt) {
		name = n;
		text = txt;
	}

	public bool canCreateAdjustable() {
		return choice == null
			&& checkbox == null
			&& button == null
			&& textArea == null
			&& widget == null
			&& !noSliders;
	}

	public int changeFlag(int flags, int bit) {
		if (checkbox != null && checkbox.Checked)
			return flags | bit;
		return flags & ~bit;
	}

	public static string makeLink(string file, string text) {
		/// TODO: CirSim.LS
		return "";
		//return "<a href=\"" + file + "\" target=\"_blank\">" + CirSim.LS(text) + "</a>";
	}
}
