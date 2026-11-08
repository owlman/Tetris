namespace owlgame
{
    partial class gamehelp
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.card = new System.Windows.Forms.Panel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.rotateCaption = new System.Windows.Forms.Label();
            this.rotateKey = new System.Windows.Forms.Label();
            this.leftCaption = new System.Windows.Forms.Label();
            this.leftKey = new System.Windows.Forms.Label();
            this.rightCaption = new System.Windows.Forms.Label();
            this.rightKey = new System.Windows.Forms.Label();
            this.downCaption = new System.Windows.Forms.Label();
            this.downKey = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.card.SuspendLayout();
            this.SuspendLayout();
            //
            // card
            //
            this.card.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.card.BackColor = System.Drawing.Color.White;
            this.card.Controls.Add(this.titleLabel);
            this.card.Controls.Add(this.rotateCaption);
            this.card.Controls.Add(this.rotateKey);
            this.card.Controls.Add(this.leftCaption);
            this.card.Controls.Add(this.leftKey);
            this.card.Controls.Add(this.rightCaption);
            this.card.Controls.Add(this.rightKey);
            this.card.Controls.Add(this.downCaption);
            this.card.Controls.Add(this.downKey);
            this.card.Controls.Add(this.button1);
            this.card.Location = new System.Drawing.Point(12, 12);
            this.card.Name = "card";
            this.card.Size = new System.Drawing.Size(276, 264);
            this.card.TabIndex = 0;
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.BackColor = System.Drawing.Color.Transparent;
            this.titleLabel.Font = new System.Drawing.Font("微软雅黑", 8.5F);
            this.titleLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(130)))), ((int)(((byte)(140)))));
            this.titleLabel.Location = new System.Drawing.Point(16, 14);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(58, 16);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "键盘操作";
            //
            // rotateCaption / rotateKey
            //
            this.rotateCaption.AutoSize = true;
            this.rotateCaption.BackColor = System.Drawing.Color.Transparent;
            this.rotateCaption.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.rotateCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            this.rotateCaption.Location = new System.Drawing.Point(16, 50);
            this.rotateCaption.Name = "rotateCaption";
            this.rotateCaption.Size = new System.Drawing.Size(36, 19);
            this.rotateCaption.Text = "旋转";
            this.rotateKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(242)))), ((int)(((byte)(246)))));
            this.rotateKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rotateKey.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.rotateKey.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(72)))));
            this.rotateKey.Location = new System.Drawing.Point(198, 46);
            this.rotateKey.Name = "rotateKey";
            this.rotateKey.Size = new System.Drawing.Size(58, 28);
            this.rotateKey.TabIndex = 1;
            this.rotateKey.Text = "↑";
            this.rotateKey.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // leftCaption / leftKey
            //
            this.leftCaption.AutoSize = true;
            this.leftCaption.BackColor = System.Drawing.Color.Transparent;
            this.leftCaption.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.leftCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            this.leftCaption.Location = new System.Drawing.Point(16, 92);
            this.leftCaption.Name = "leftCaption";
            this.leftCaption.Size = new System.Drawing.Size(36, 19);
            this.leftCaption.Text = "左移";
            this.leftKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(242)))), ((int)(((byte)(246)))));
            this.leftKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.leftKey.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.leftKey.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(72)))));
            this.leftKey.Location = new System.Drawing.Point(198, 88);
            this.leftKey.Name = "leftKey";
            this.leftKey.Size = new System.Drawing.Size(58, 28);
            this.leftKey.TabIndex = 2;
            this.leftKey.Text = "←";
            this.leftKey.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // rightCaption / rightKey
            //
            this.rightCaption.AutoSize = true;
            this.rightCaption.BackColor = System.Drawing.Color.Transparent;
            this.rightCaption.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.rightCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            this.rightCaption.Location = new System.Drawing.Point(16, 134);
            this.rightCaption.Name = "rightCaption";
            this.rightCaption.Size = new System.Drawing.Size(36, 19);
            this.rightCaption.Text = "右移";
            this.rightKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(242)))), ((int)(((byte)(246)))));
            this.rightKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rightKey.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.rightKey.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(72)))));
            this.rightKey.Location = new System.Drawing.Point(198, 130);
            this.rightKey.Name = "rightKey";
            this.rightKey.Size = new System.Drawing.Size(58, 28);
            this.rightKey.TabIndex = 3;
            this.rightKey.Text = "→";
            this.rightKey.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // downCaption / downKey
            //
            this.downCaption.AutoSize = true;
            this.downCaption.BackColor = System.Drawing.Color.Transparent;
            this.downCaption.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.downCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            this.downCaption.Location = new System.Drawing.Point(16, 176);
            this.downCaption.Name = "downCaption";
            this.downCaption.Size = new System.Drawing.Size(36, 19);
            this.downCaption.Text = "加速";
            this.downKey.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(242)))), ((int)(((byte)(246)))));
            this.downKey.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.downKey.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold);
            this.downKey.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(72)))));
            this.downKey.Location = new System.Drawing.Point(198, 172);
            this.downKey.Name = "downKey";
            this.downKey.Size = new System.Drawing.Size(58, 28);
            this.downKey.TabIndex = 4;
            this.downKey.Text = "↓";
            this.downKey.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // button1
            //
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(110)))), ((int)(((byte)(220)))));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(98)))), ((int)(((byte)(196)))));
            this.button1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(84)))), ((int)(((byte)(170)))));
            this.button1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(124)))), ((int)(((byte)(235)))));
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("微软雅黑", 9.5F);
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(94, 216);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(88, 34);
            this.button1.TabIndex = 5;
            this.button1.Text = "确定";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // gamehelp
            //
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(243)))), ((int)(((byte)(246)))));
            this.ClientSize = new System.Drawing.Size(300, 288);
            this.Controls.Add(this.card);
            this.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "gamehelp";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "操作说明";
            this.card.ResumeLayout(false);
            this.card.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel card;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label rotateCaption;
        private System.Windows.Forms.Label rotateKey;
        private System.Windows.Forms.Label leftCaption;
        private System.Windows.Forms.Label leftKey;
        private System.Windows.Forms.Label rightCaption;
        private System.Windows.Forms.Label rightKey;
        private System.Windows.Forms.Label downCaption;
        private System.Windows.Forms.Label downKey;
        private System.Windows.Forms.Button button1;
    }
}
