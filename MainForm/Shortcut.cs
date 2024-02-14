struct Shortcut {
	public readonly Keys Key;
	public readonly string Name;

	public Shortcut(Keys k, bool ctrl = true, bool shift = false, bool alt = false) {
		Key = k;
		Name = "";
		if (k != Keys.None) {
			if (alt || 0 < (k & Keys.Alt)) {
				Key |= Keys.Alt;
				Name = "Alt+";
			}
			if (ctrl || 0 < (k & Keys.Control)) {
				Key |= Keys.Control;
				Name = "Ctrl+";
			}
			if (shift || 0 < (k & Keys.Shift)) {
				Key |= Keys.Shift;
				Name = "Shift+";
			}
			Name += name(k);
		}
	}

	static string name(Keys k) {
		switch (k) {
		case Keys.D0:
		case Keys.D1:
		case Keys.D2:
		case Keys.D3:
		case Keys.D4:
		case Keys.D5:
		case Keys.D6:
		case Keys.D7:
		case Keys.D8:
		case Keys.D9:
			return k.ToString().Replace("D", "");
		case Keys.Delete:
			return "Del";
		case Keys.Oemplus:
			return "+";
		case Keys.OemMinus:
			return "-";
		default:
			return k.ToString();
		}
	}
}
