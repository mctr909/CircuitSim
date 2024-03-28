namespace MainForm {
	internal class CustomLogicModel {
		public static string escape(string s) {
			if (s.Length == 0) {
				return "\\0";
			}
			return s.Replace("\\", "\\\\")
				.Replace("\n", "\\n")
				.Replace(" ", "\\s")
				.Replace("+", "\\p")
				.Replace("=", "\\q")
				.Replace("#", "\\h")
				.Replace("&", "\\a")
				.Replace("\r", "\\r");
		}

		public static string unescape(string s) {
			if (s == "\\0") {
				return "";
			}
			for (int i = 0; i < s.Length; i++) {
				if (s.ElementAt(i) == '\\') {
					var c = s.ElementAt(i + 1);
					s = c switch
					{
						'n' => $"{s[..i]}\n{s[(i + 2)..]}",
						'r' => $"{s[..i]}\r{s[(i + 2)..]}",
						's' => $"{s[..i]} {s[(i + 2)..]}",
						'p' => $"{s[..i]}+{s[(i + 2)..]}",
						'q' => $"{s[..i]}={s[(i + 2)..]}",
						'h' => $"{s[..i]}#{s[(i + 2)..]}",
						'a' => $"{s[..i]}&{s[(i + 2)..]}",
						_ => $"{s[..i]}{s[(i + 1)..]}",
					};
				}
			}
			return s;
		}
	}
}
