using System.Collections.Generic;

namespace Circuit {
    class Storage {
        static Storage mIns = null;

        Dictionary<string, string> mData = new Dictionary<string, string>();

        private Storage() { }

        public static Storage GetInstance() {
            if (null == mIns) {
                mIns = new Storage();
            }
            return mIns;
        }

        public void SetItem(string k, string v) {
            if (mData.ContainsKey(k)) {
                mData[k] = v;
            } else {
                mData.Add(k, v);
            }
        }

        public string GetItem(string k) {
            if (mData.ContainsKey(k)) {
                return mData[k];
            } else {
                return "";
            }
        }
    }
}
