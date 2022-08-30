﻿namespace Circuit.Forms {
    partial class ScopeProperties {
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
            this.chkScale = new System.Windows.Forms.CheckBox();
            this.chkManualScale = new System.Windows.Forms.CheckBox();
            this.chkPeak = new System.Windows.Forms.CheckBox();
            this.chkNegPeak = new System.Windows.Forms.CheckBox();
            this.chkFreq = new System.Windows.Forms.CheckBox();
            this.chkLogSpectrum = new System.Windows.Forms.CheckBox();
            this.chkRms = new System.Windows.Forms.CheckBox();
            this.txtManualScale = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cmbColor = new System.Windows.Forms.ComboBox();
            this.lblColor = new System.Windows.Forms.Label();
            this.rbSpectrum = new System.Windows.Forms.RadioButton();
            this.rbVoltage = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtLabel = new System.Windows.Forms.TextBox();
            this.tbSpeed = new System.Windows.Forms.TrackBar();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.lblScopeSpeed = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbSpeed)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkScale
            // 
            this.chkScale.AutoSize = true;
            this.chkScale.Location = new System.Drawing.Point(6, 18);
            this.chkScale.Name = "chkScale";
            this.chkScale.Size = new System.Drawing.Size(62, 16);
            this.chkScale.TabIndex = 0;
            this.chkScale.Text = "スケール";
            this.chkScale.UseVisualStyleBackColor = true;
            this.chkScale.CheckedChanged += new System.EventHandler(this.chkScale_CheckedChanged);
            // 
            // chkManualScale
            // 
            this.chkManualScale.AutoSize = true;
            this.chkManualScale.Location = new System.Drawing.Point(74, 71);
            this.chkManualScale.Name = "chkManualScale";
            this.chkManualScale.Size = new System.Drawing.Size(120, 16);
            this.chkManualScale.TabIndex = 1;
            this.chkManualScale.Text = "スケールの手動設定";
            this.chkManualScale.UseVisualStyleBackColor = true;
            this.chkManualScale.CheckedChanged += new System.EventHandler(this.chkManualScale_CheckedChanged);
            // 
            // chkPeak
            // 
            this.chkPeak.AutoSize = true;
            this.chkPeak.Location = new System.Drawing.Point(84, 18);
            this.chkPeak.Name = "chkPeak";
            this.chkPeak.Size = new System.Drawing.Size(60, 16);
            this.chkPeak.TabIndex = 3;
            this.chkPeak.Text = "最大値";
            this.chkPeak.UseVisualStyleBackColor = true;
            this.chkPeak.CheckedChanged += new System.EventHandler(this.chkPeak_CheckedChanged);
            // 
            // chkNegPeak
            // 
            this.chkNegPeak.AutoSize = true;
            this.chkNegPeak.Location = new System.Drawing.Point(84, 40);
            this.chkNegPeak.Name = "chkNegPeak";
            this.chkNegPeak.Size = new System.Drawing.Size(60, 16);
            this.chkNegPeak.TabIndex = 4;
            this.chkNegPeak.Text = "最小値";
            this.chkNegPeak.UseVisualStyleBackColor = true;
            this.chkNegPeak.CheckedChanged += new System.EventHandler(this.chkNegPeak_CheckedChanged);
            // 
            // chkFreq
            // 
            this.chkFreq.AutoSize = true;
            this.chkFreq.Location = new System.Drawing.Point(6, 40);
            this.chkFreq.Name = "chkFreq";
            this.chkFreq.Size = new System.Drawing.Size(60, 16);
            this.chkFreq.TabIndex = 5;
            this.chkFreq.Text = "周波数";
            this.chkFreq.UseVisualStyleBackColor = true;
            this.chkFreq.CheckedChanged += new System.EventHandler(this.chkFreq_CheckedChanged);
            // 
            // chkLogSpectrum
            // 
            this.chkLogSpectrum.AutoSize = true;
            this.chkLogSpectrum.Location = new System.Drawing.Point(84, 18);
            this.chkLogSpectrum.Name = "chkLogSpectrum";
            this.chkLogSpectrum.Size = new System.Drawing.Size(105, 16);
            this.chkLogSpectrum.TabIndex = 7;
            this.chkLogSpectrum.Text = "振幅を対数表示";
            this.chkLogSpectrum.UseVisualStyleBackColor = true;
            this.chkLogSpectrum.CheckedChanged += new System.EventHandler(this.chkLogSpectrum_CheckedChanged);
            // 
            // chkRms
            // 
            this.chkRms.AutoSize = true;
            this.chkRms.Location = new System.Drawing.Point(6, 62);
            this.chkRms.Name = "chkRms";
            this.chkRms.Size = new System.Drawing.Size(48, 16);
            this.chkRms.TabIndex = 8;
            this.chkRms.Text = "RMS";
            this.chkRms.UseVisualStyleBackColor = true;
            this.chkRms.CheckedChanged += new System.EventHandler(this.chkRms_CheckedChanged);
            // 
            // txtManualScale
            // 
            this.txtManualScale.Location = new System.Drawing.Point(6, 69);
            this.txtManualScale.Name = "txtManualScale";
            this.txtManualScale.Size = new System.Drawing.Size(62, 19);
            this.txtManualScale.TabIndex = 9;
            this.txtManualScale.TextChanged += new System.EventHandler(this.txtManualScale_TextChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cmbColor);
            this.groupBox1.Controls.Add(this.lblColor);
            this.groupBox1.Controls.Add(this.rbSpectrum);
            this.groupBox1.Controls.Add(this.rbVoltage);
            this.groupBox1.Controls.Add(this.chkLogSpectrum);
            this.groupBox1.Location = new System.Drawing.Point(4, 106);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(195, 86);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "表示するグラフ";
            // 
            // cmbColor
            // 
            this.cmbColor.FormattingEnabled = true;
            this.cmbColor.Location = new System.Drawing.Point(84, 57);
            this.cmbColor.Name = "cmbColor";
            this.cmbColor.Size = new System.Drawing.Size(100, 20);
            this.cmbColor.TabIndex = 12;
            this.cmbColor.SelectedIndexChanged += new System.EventHandler(this.cmbColor_SelectedIndexChanged);
            // 
            // lblColor
            // 
            this.lblColor.AutoSize = true;
            this.lblColor.Location = new System.Drawing.Point(82, 42);
            this.lblColor.Name = "lblColor";
            this.lblColor.Size = new System.Drawing.Size(51, 12);
            this.lblColor.TabIndex = 11;
            this.lblColor.Text = "波形の色";
            // 
            // rbSpectrum
            // 
            this.rbSpectrum.AutoSize = true;
            this.rbSpectrum.Location = new System.Drawing.Point(6, 18);
            this.rbSpectrum.Name = "rbSpectrum";
            this.rbSpectrum.Size = new System.Drawing.Size(76, 16);
            this.rbSpectrum.TabIndex = 9;
            this.rbSpectrum.TabStop = true;
            this.rbSpectrum.Text = "スペクトラム";
            this.rbSpectrum.UseVisualStyleBackColor = true;
            this.rbSpectrum.CheckedChanged += new System.EventHandler(this.rbSpectrum_CheckedChanged);
            // 
            // rbVoltage
            // 
            this.rbVoltage.AutoSize = true;
            this.rbVoltage.Location = new System.Drawing.Point(6, 40);
            this.rbVoltage.Name = "rbVoltage";
            this.rbVoltage.Size = new System.Drawing.Size(71, 16);
            this.rbVoltage.TabIndex = 8;
            this.rbVoltage.TabStop = true;
            this.rbVoltage.Text = "電圧波形";
            this.rbVoltage.UseVisualStyleBackColor = true;
            this.rbVoltage.CheckedChanged += new System.EventHandler(this.rbVoltage_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.txtLabel);
            this.groupBox2.Controls.Add(this.chkScale);
            this.groupBox2.Controls.Add(this.chkFreq);
            this.groupBox2.Controls.Add(this.chkRms);
            this.groupBox2.Controls.Add(this.chkNegPeak);
            this.groupBox2.Controls.Add(this.chkPeak);
            this.groupBox2.Location = new System.Drawing.Point(4, 198);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(195, 103);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "表示する値";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(82, 63);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 12);
            this.label1.TabIndex = 10;
            this.label1.Text = "スコープのラベル";
            // 
            // txtLabel
            // 
            this.txtLabel.Location = new System.Drawing.Point(84, 78);
            this.txtLabel.Name = "txtLabel";
            this.txtLabel.Size = new System.Drawing.Size(100, 19);
            this.txtLabel.TabIndex = 9;
            this.txtLabel.TextChanged += new System.EventHandler(this.txtLabel_TextChanged);
            // 
            // tbSpeed
            // 
            this.tbSpeed.Location = new System.Drawing.Point(6, 18);
            this.tbSpeed.Name = "tbSpeed";
            this.tbSpeed.Size = new System.Drawing.Size(188, 45);
            this.tbSpeed.TabIndex = 12;
            this.tbSpeed.ValueChanged += new System.EventHandler(this.tbSpeed_ValueChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.lblScopeSpeed);
            this.groupBox3.Controls.Add(this.chkManualScale);
            this.groupBox3.Controls.Add(this.txtManualScale);
            this.groupBox3.Controls.Add(this.tbSpeed);
            this.groupBox3.Location = new System.Drawing.Point(4, 4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(195, 96);
            this.groupBox3.TabIndex = 13;
            this.groupBox3.TabStop = false;
            // 
            // lblScopeSpeed
            // 
            this.lblScopeSpeed.AutoSize = true;
            this.lblScopeSpeed.Location = new System.Drawing.Point(118, 51);
            this.lblScopeSpeed.Name = "lblScopeSpeed";
            this.lblScopeSpeed.Size = new System.Drawing.Size(71, 12);
            this.lblScopeSpeed.TabIndex = 13;
            this.lblScopeSpeed.Text = "999.99uS/div";
            // 
            // ScopeProperties
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(203, 304);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ScopeProperties";
            this.Text = "ScopeProperties";
            this.Load += new System.EventHandler(this.ScopeProperties_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.tbSpeed)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox chkScale;
        private System.Windows.Forms.CheckBox chkManualScale;
        private System.Windows.Forms.CheckBox chkPeak;
        private System.Windows.Forms.CheckBox chkNegPeak;
        private System.Windows.Forms.CheckBox chkFreq;
        private System.Windows.Forms.CheckBox chkLogSpectrum;
        private System.Windows.Forms.CheckBox chkRms;
        private System.Windows.Forms.TextBox txtManualScale;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TrackBar tbSpeed;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox txtLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblScopeSpeed;
        private System.Windows.Forms.RadioButton rbSpectrum;
        private System.Windows.Forms.RadioButton rbVoltage;
        private System.Windows.Forms.ComboBox cmbColor;
        private System.Windows.Forms.Label lblColor;
    }
}