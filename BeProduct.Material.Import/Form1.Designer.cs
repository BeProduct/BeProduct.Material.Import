namespace BeProduct.Material.Import
{
    partial class Form1
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
            this.btLogin = new System.Windows.Forms.Button();
            this.lbLogin = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.btUpload = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btLogin
            // 
            this.btLogin.Location = new System.Drawing.Point(1089, 284);
            this.btLogin.Name = "btLogin";
            this.btLogin.Size = new System.Drawing.Size(231, 134);
            this.btLogin.TabIndex = 0;
            this.btLogin.Text = "Login";
            this.btLogin.UseVisualStyleBackColor = true;
            this.btLogin.Click += new System.EventHandler(this.btLogin_Click);
            // 
            // lbLogin
            // 
            this.lbLogin.AutoSize = true;
            this.lbLogin.Location = new System.Drawing.Point(990, 207);
            this.lbLogin.Name = "lbLogin";
            this.lbLogin.Size = new System.Drawing.Size(93, 32);
            this.lbLogin.TabIndex = 1;
            this.lbLogin.Text = "label1";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(205, 63);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(959, 61);
            this.progressBar1.TabIndex = 2;
            // 
            // btUpload
            // 
            this.btUpload.Location = new System.Drawing.Point(374, 187);
            this.btUpload.Name = "btUpload";
            this.btUpload.Size = new System.Drawing.Size(476, 150);
            this.btUpload.TabIndex = 3;
            this.btUpload.Text = "Import CSV";
            this.btUpload.UseVisualStyleBackColor = true;
            this.btUpload.Click += new System.EventHandler(this.btImport_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(16F, 31F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1332, 430);
            this.Controls.Add(this.btUpload);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.lbLogin);
            this.Controls.Add(this.btLogin);
            this.Name = "Form1";
            this.Text = "BeProduct Material Import";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btLogin;
        private System.Windows.Forms.Label lbLogin;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btUpload;
    }
}

