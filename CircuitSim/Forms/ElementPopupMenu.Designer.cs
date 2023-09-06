namespace Circuit.Forms {
    partial class ElementPopupMenu {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.Edit = new System.Windows.Forms.ToolStripMenuItem();
            this.ScopeWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.ScopeFloat = new System.Windows.Forms.ToolStripMenuItem();
            this.SplitWire = new System.Windows.Forms.ToolStripMenuItem();
            this.FlipPosts = new System.Windows.Forms.ToolStripMenuItem();
            this.Slider = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.SuspendLayout();
            // 
            // Edit
            // 
            this.Edit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.Edit.Name = "Edit";
            this.Edit.Size = new System.Drawing.Size(98, 22);
            this.Edit.Text = "編集";
            // 
            // ScopeWindow
            // 
            this.ScopeWindow.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ScopeWindow.Name = "ScopeWindow";
            this.ScopeWindow.Size = new System.Drawing.Size(98, 22);
            this.ScopeWindow.Text = "スコープウィンドウに波形を表示";
            // 
            // ScopeFloat
            // 
            this.ScopeFloat.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.ScopeFloat.Name = "ScopeFloat";
            this.ScopeFloat.Size = new System.Drawing.Size(98, 22);
            this.ScopeFloat.Text = "任意の場所に波形を表示";
            // 
            // SplitWire
            // 
            this.SplitWire.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.SplitWire.Name = "SplitWire";
            this.SplitWire.Size = new System.Drawing.Size(98, 22);
            this.SplitWire.Text = "ワイヤーを分割";
            // 
            // FlipPosts
            // 
            this.FlipPosts.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.FlipPosts.Name = "FlipPosts";
            this.FlipPosts.Size = new System.Drawing.Size(98, 22);
            this.FlipPosts.Text = "端子を入れ替える";
            // 
            // Slider
            // 
            this.Slider.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.Slider.Name = "Slider";
            this.Slider.Size = new System.Drawing.Size(98, 22);
            this.Slider.Text = "スライダーの設定";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(95, 6);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(95, 6);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(95, 6);
            // 
            // ElementMenu
            // 
            this.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Edit,
            this.toolStripSeparator1,
            this.ScopeWindow,
            this.ScopeFloat,
            this.toolStripSeparator2,
            this.SplitWire,
            this.FlipPosts,
            this.toolStripSeparator3,
            this.Slider});
            this.Size = new System.Drawing.Size(99, 154);
            this.Text = "素子メニュー";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripMenuItem Edit;
        private System.Windows.Forms.ToolStripMenuItem ScopeWindow;
        private System.Windows.Forms.ToolStripMenuItem ScopeFloat;
        private System.Windows.Forms.ToolStripMenuItem SplitWire;
        private System.Windows.Forms.ToolStripMenuItem FlipPosts;
        private System.Windows.Forms.ToolStripMenuItem Slider;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
    }
}
