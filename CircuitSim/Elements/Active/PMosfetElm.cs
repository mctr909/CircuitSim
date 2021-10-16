﻿using System.Drawing;

namespace Circuit.Elements.Active {
    class PMosfetElm : MosfetElm {
        public PMosfetElm(Point pos) : base(pos, true) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.PMOS; } }
    }
}