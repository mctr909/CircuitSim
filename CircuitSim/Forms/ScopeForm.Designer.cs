namespace Circuit.Forms {
    partial class ScopeForm {
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
            this.picScope = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.picScope)).BeginInit();
            this.SuspendLayout();
            // 
            // picScope
            // 
            this.picScope.BackColor = System.Drawing.Color.Black;
            this.picScope.Location = new System.Drawing.Point(0, 0);
            this.picScope.Name = "picScope";
            this.picScope.Size = new System.Drawing.Size(100, 50);
            this.picScope.TabIndex = 0;
            this.picScope.TabStop = false;
            this.picScope.Click += new System.EventHandler(this.picScope_Click);
            this.picScope.DoubleClick += new System.EventHandler(this.picScope_DoubleClick);
            this.picScope.MouseLeave += new System.EventHandler(this.picScope_MouseLeave);
            this.picScope.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picScope_MouseMove);
            // 
            // ScopeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(380, 351);
            this.ControlBox = false;
            this.Controls.Add(this.picScope);
            this.MinimumSize = new System.Drawing.Size(128, 64);
            this.Name = "ScopeForm";
            this.Text = "ScopeForm";
            this.SizeChanged += new System.EventHandler(this.ScopeForm_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.picScope)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox picScope;
    }
}