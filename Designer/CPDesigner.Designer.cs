namespace Designer
{
    partial class CPDesigner
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lbListeners = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tbListener = new System.Windows.Forms.TextBox();
            this.lbParameters = new System.Windows.Forms.ListBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lbContexts = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.lbActiveListeners = new System.Windows.Forms.ListBox();
            this.label6 = new System.Windows.Forms.Label();
            this.lbContextTypeMaps = new System.Windows.Forms.ListBox();
            this.pnlDiagram = new System.Windows.Forms.Panel();
            this.label7 = new System.Windows.Forms.Label();
            this.lbPublishes = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // lbListeners
            // 
            this.lbListeners.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lbListeners.FormattingEnabled = true;
            this.lbListeners.Location = new System.Drawing.Point(12, 47);
            this.lbListeners.Name = "lbListeners";
            this.lbListeners.Size = new System.Drawing.Size(190, 589);
            this.lbListeners.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Listeners:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(217, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Parameters:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(217, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Listener:";
            // 
            // tbListener
            // 
            this.tbListener.Location = new System.Drawing.Point(286, 25);
            this.tbListener.Name = "tbListener";
            this.tbListener.ReadOnly = true;
            this.tbListener.Size = new System.Drawing.Size(140, 20);
            this.tbListener.TabIndex = 5;
            // 
            // lbParameters
            // 
            this.lbParameters.FormattingEnabled = true;
            this.lbParameters.Location = new System.Drawing.Point(286, 51);
            this.lbParameters.Name = "lbParameters";
            this.lbParameters.Size = new System.Drawing.Size(260, 82);
            this.lbParameters.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(217, 147);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(51, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Contexts:";
            // 
            // lbContexts
            // 
            this.lbContexts.FormattingEnabled = true;
            this.lbContexts.Location = new System.Drawing.Point(220, 163);
            this.lbContexts.Name = "lbContexts";
            this.lbContexts.Size = new System.Drawing.Size(165, 225);
            this.lbContexts.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(388, 147);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(82, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "ActiveListeners:";
            // 
            // lbActiveListeners
            // 
            this.lbActiveListeners.FormattingEnabled = true;
            this.lbActiveListeners.Location = new System.Drawing.Point(391, 163);
            this.lbActiveListeners.Name = "lbActiveListeners";
            this.lbActiveListeners.Size = new System.Drawing.Size(149, 108);
            this.lbActiveListeners.TabIndex = 10;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(217, 408);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(102, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Context Type Maps:";
            // 
            // lbContextTypeMaps
            // 
            this.lbContextTypeMaps.FormattingEnabled = true;
            this.lbContextTypeMaps.Location = new System.Drawing.Point(220, 424);
            this.lbContextTypeMaps.Name = "lbContextTypeMaps";
            this.lbContextTypeMaps.Size = new System.Drawing.Size(165, 212);
            this.lbContextTypeMaps.TabIndex = 12;
            // 
            // pnlDiagram
            // 
            this.pnlDiagram.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlDiagram.Location = new System.Drawing.Point(392, 278);
            this.pnlDiagram.Name = "pnlDiagram";
            this.pnlDiagram.Size = new System.Drawing.Size(627, 358);
            this.pnlDiagram.TabIndex = 13;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(577, 50);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(55, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Publishes:";
            // 
            // lbPublishes
            // 
            this.lbPublishes.FormattingEnabled = true;
            this.lbPublishes.Location = new System.Drawing.Point(638, 51);
            this.lbPublishes.Name = "lbPublishes";
            this.lbPublishes.Size = new System.Drawing.Size(130, 82);
            this.lbPublishes.TabIndex = 15;
            // 
            // CPDesigner
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1031, 647);
            this.Controls.Add(this.lbPublishes);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.pnlDiagram);
            this.Controls.Add(this.lbContextTypeMaps);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lbActiveListeners);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lbContexts);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lbParameters);
            this.Controls.Add(this.tbListener);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lbListeners);
            this.Name = "CPDesigner";
            this.Text = "Context Computing Designer";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lbListeners;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbListener;
        private System.Windows.Forms.ListBox lbParameters;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox lbContexts;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox lbActiveListeners;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ListBox lbContextTypeMaps;
        private System.Windows.Forms.Panel pnlDiagram;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ListBox lbPublishes;
    }
}

