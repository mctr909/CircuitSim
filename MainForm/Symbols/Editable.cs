namespace MainForm;

internal interface Editable {
	public EditInfo? getEditInfo(int n);

	public void setEditValue(int n, EditInfo ei);
}
