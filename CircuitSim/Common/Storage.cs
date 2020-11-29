using System.Collections.Generic;

namespace Circuit {
    class Storage {
        static Storage ins = null;

        Dictionary<string, string> data = new Dictionary<string, string>();

        private Storage() { }

        public static Storage getLocalStorageIfSupported() {
            if (null == ins) {
                ins = new Storage();
            }
            return ins;
        }

        public void setItem(string k, string v) {
            if (data.ContainsKey(k)) {
                data[k] = v;
            } else {
                data.Add(k, v);
            }
        }

        public string getItem(string k) {
            if (data.ContainsKey(k)) {
                return data[k];
            } else {
                return "";
            }
        }
    }
}
