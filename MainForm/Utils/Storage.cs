namespace MainForm;

internal class Storage {
	private static Dictionary<string, string> mItems = [];

	public static void setItem(string key, string text) {
		if (!mItems.TryAdd(key, text))
			mItems[key] = text;
	}

	public static string getItem(string key) {
		return mItems.TryGetValue(key, out var value) ? value : "";
	}
}
