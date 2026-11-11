namespace owlgame
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.gamewiew = new System.Windows.Forms.Panel();
            this.gaizi = new System.Windows.Forms.Panel();
            this.welcomelabel = new System.Windows.Forms.Label();
            this.previewCard = new System.Windows.Forms.Panel();
            this.previewTitle = new System.Windows.Forms.Label();
            this.proimg = new System.Windows.Forms.PictureBox();
            this.controlCard = new System.Windows.Forms.Panel();
            this.controlTitle = new System.Windows.Forms.Label();
            this.myweb = new System.Windows.Forms.LinkLabel();
            this.buttonexit = new System.Windows.Forms.Button();
            this.buttonback = new System.Windows.Forms.Button();
            this.buttonstart = new System.Windows.Forms.Button();
            this.buttonset = new System.Windows.Forms.Button();
            this.scoreCard = new System.Windows.Forms.Panel();
            this.scoreCaption = new System.Windows.Forms.Label();
            this.scoreValue = new System.Windows.Forms.Label();
            this.levelCaption = new System.Windows.Forms.Label();
            this.levelValue = new System.Windows.Forms.Label();
            this.linesCaption = new System.Windows.Forms.Label();
            this.linesValue = new System.Windows.Forms.Label();
            this.gametimer = new System.Windows.Forms.Timer(this.components);
            this.flashTimer = new System.Windows.Forms.Timer(this.components);
            this.gamewiew.SuspendLayout();
            this.gaizi.SuspendLayout();
            this.previewCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.proimg)).BeginInit();
            this.controlCard.SuspendLayout();
            this.scoreCard.SuspendLayout();
            this.SuspendLayout();
            //
            // gamewiew
            //
            // Size is deliberately kept at 220 x 388: the number of board lines is
            // derived from this aspect ratio (20 * H / W = 35) and the pixel tests
            // pin that geometry. Modernising the chrome must not move it.
            this.gamewiew.AutoSize = false;
            this.gamewiew.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(32)))));
            this.gamewiew.Controls.Add(this.gaizi);
            this.gamewiew.Location = new System.Drawing.Point(12, 12);
            this.gamewiew.Name = "gamewiew";
            this.gamewiew.Size = new System.Drawing.Size(220, 388);
            this.gamewiew.TabIndex = 0;
            this.gamewiew.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.gamewiew_PreviewKeyDown);
            this.gamewiew.Paint += new System.Windows.Forms.PaintEventHandler(this.gamewiew_Paint);
            //
            // gaizi
            //
            this.gaizi.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(32)))));
            this.gaizi.Controls.Add(this.welcomelabel);
            this.gaizi.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gaizi.Location = new System.Drawing.Point(0, 0);
            this.gaizi.Name = "gaizi";
            this.gaizi.Size = new System.Drawing.Size(220, 388);
            this.gaizi.TabIndex = 0;
            //
            // welcomelabel
            //
            this.welcomelabel.AutoSize = false;
            this.welcomelabel.BackColor = System.Drawing.Color.Transparent;
            this.welcomelabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.welcomelabel.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 15F);
            this.welcomelabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.welcomelabel.Location = new System.Drawing.Point(0, 0);
            this.welcomelabel.Name = "welcomelabel";
            this.welcomelabel.Size = new System.Drawing.Size(220, 388);
            this.welcomelabel.TabIndex = 0;
            this.welcomelabel.Text = owlgame.Strings.Welcome;
            this.welcomelabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // previewCard
            //
            this.previewCard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.previewCard.BackColor = System.Drawing.Color.White;
            this.previewCard.Controls.Add(this.proimg);
            this.previewCard.Controls.Add(this.previewTitle);
            this.previewCard.Location = new System.Drawing.Point(244, 12);
            this.previewCard.Name = "previewCard";
            this.previewCard.Size = new System.Drawing.Size(112, 112);
            this.previewCard.TabIndex = 1;
            //
            // previewTitle
            //
            this.previewTitle.AutoSize = true;
            this.previewTitle.BackColor = System.Drawing.Color.Transparent;
            this.previewTitle.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 8.5F);
            this.previewTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(130)))), ((int)(((byte)(140)))));
            this.previewTitle.Location = new System.Drawing.Point(12, 10);
            this.previewTitle.Name = "previewTitle";
            this.previewTitle.Size = new System.Drawing.Size(44, 16);
            this.previewTitle.TabIndex = 0;
            this.previewTitle.Text = owlgame.Strings.PreviewTitle;
            //
            // proimg
            //
            this.proimg.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(32)))));
            this.proimg.Location = new System.Drawing.Point(27, 34);
            this.proimg.Name = "proimg";
            this.proimg.Size = new System.Drawing.Size(58, 58);
            this.proimg.TabIndex = 1;
            this.proimg.TabStop = false;
            this.proimg.Paint += new System.Windows.Forms.PaintEventHandler(this.proimg_Paint);
            //
            // controlCard
            //
            this.controlCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.controlCard.BackColor = System.Drawing.Color.White;
            this.controlCard.Controls.Add(this.buttonset);
            this.controlCard.Controls.Add(this.buttonstart);
            this.controlCard.Controls.Add(this.buttonback);
            this.controlCard.Controls.Add(this.buttonexit);
            this.controlCard.Controls.Add(this.myweb);
            this.controlCard.Controls.Add(this.controlTitle);
            this.controlCard.Location = new System.Drawing.Point(244, 132);
            this.controlCard.Name = "controlCard";
            this.controlCard.Size = new System.Drawing.Size(112, 268);
            this.controlCard.TabIndex = 2;
            //
            // controlTitle
            //
            this.controlTitle.AutoSize = true;
            this.controlTitle.BackColor = System.Drawing.Color.Transparent;
            this.controlTitle.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 8.5F);
            this.controlTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(130)))), ((int)(((byte)(140)))));
            this.controlTitle.Location = new System.Drawing.Point(12, 10);
            this.controlTitle.Name = "controlTitle";
            this.controlTitle.Size = new System.Drawing.Size(52, 16);
            this.controlTitle.TabIndex = 0;
            this.controlTitle.Text = owlgame.Strings.ControlTitle;
            //
            // buttonset
            //
            this.buttonset.BackColor = System.Drawing.Color.White;
            this.buttonset.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(216)))), ((int)(((byte)(224)))));
            this.buttonset.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(228)))), ((int)(((byte)(228)))), ((int)(((byte)(236)))));
            this.buttonset.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(243)))), ((int)(((byte)(248)))));
            this.buttonset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonset.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 9F);
            this.buttonset.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            this.buttonset.Location = new System.Drawing.Point(12, 34);
            this.buttonset.Name = "buttonset";
            this.buttonset.Size = new System.Drawing.Size(88, 34);
            this.buttonset.TabIndex = 1;
            this.buttonset.Text = owlgame.Strings.HelpButton;
            this.buttonset.UseVisualStyleBackColor = false;
            this.buttonset.Click += new System.EventHandler(this.buttonset_Click);
            //
            // buttonstart
            //
            this.buttonstart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(110)))), ((int)(((byte)(220)))));
            this.buttonstart.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(98)))), ((int)(((byte)(196)))));
            this.buttonstart.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(84)))), ((int)(((byte)(170)))));
            this.buttonstart.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(124)))), ((int)(((byte)(235)))));
            this.buttonstart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonstart.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 9F);
            this.buttonstart.ForeColor = System.Drawing.Color.White;
            this.buttonstart.Location = new System.Drawing.Point(12, 78);
            this.buttonstart.Name = "buttonstart";
            this.buttonstart.Size = new System.Drawing.Size(88, 34);
            this.buttonstart.TabIndex = 2;
            this.buttonstart.Text = owlgame.Strings.StartButton;
            this.buttonstart.UseVisualStyleBackColor = false;
            this.buttonstart.Click += new System.EventHandler(this.buttonstart_Click);
            //
            // buttonback
            //
            this.buttonback.BackColor = System.Drawing.Color.White;
            this.buttonback.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(216)))), ((int)(((byte)(224)))));
            this.buttonback.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(228)))), ((int)(((byte)(228)))), ((int)(((byte)(236)))));
            this.buttonback.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(243)))), ((int)(((byte)(248)))));
            this.buttonback.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonback.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 9F);
            this.buttonback.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            this.buttonback.Location = new System.Drawing.Point(12, 122);
            this.buttonback.Name = "buttonback";
            this.buttonback.Size = new System.Drawing.Size(88, 34);
            this.buttonback.TabIndex = 3;
            this.buttonback.Text = owlgame.Strings.RestartButton;
            this.buttonback.UseVisualStyleBackColor = false;
            this.buttonback.Click += new System.EventHandler(this.buttonback_Click);
            //
            // buttonexit
            //
            this.buttonexit.BackColor = System.Drawing.Color.White;
            this.buttonexit.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(216)))), ((int)(((byte)(224)))));
            this.buttonexit.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(228)))), ((int)(((byte)(228)))), ((int)(((byte)(236)))));
            this.buttonexit.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(243)))), ((int)(((byte)(248)))));
            this.buttonexit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonexit.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 9F);
            this.buttonexit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(58)))));
            this.buttonexit.Location = new System.Drawing.Point(12, 166);
            this.buttonexit.Name = "buttonexit";
            this.buttonexit.Size = new System.Drawing.Size(88, 34);
            this.buttonexit.TabIndex = 4;
            this.buttonexit.Text = owlgame.Strings.ExitButton;
            this.buttonexit.UseVisualStyleBackColor = false;
            this.buttonexit.Click += new System.EventHandler(this.buttonexit_Click);
            //
            // myweb
            //
            this.myweb.AutoSize = true;
            this.myweb.BackColor = System.Drawing.Color.Transparent;
            this.myweb.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 8F);
            this.myweb.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.myweb.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(140)))), ((int)(((byte)(150)))));
            this.myweb.Location = new System.Drawing.Point(12, 240);
            this.myweb.Name = "myweb";
            this.myweb.Size = new System.Drawing.Size(62, 16);
            this.myweb.TabIndex = 5;
            this.myweb.TabStop = true;
            this.myweb.Tag = "www.lingjie.net";
            this.myweb.Text = owlgame.Strings.Credit;
            this.myweb.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.myweb_LinkClicked);
            //
            // scoreCard
            //
            this.scoreCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.scoreCard.BackColor = System.Drawing.Color.White;
            this.scoreCard.Controls.Add(this.scoreCaption);
            this.scoreCard.Controls.Add(this.scoreValue);
            this.scoreCard.Controls.Add(this.levelCaption);
            this.scoreCard.Controls.Add(this.levelValue);
            this.scoreCard.Controls.Add(this.linesCaption);
            this.scoreCard.Controls.Add(this.linesValue);
            this.scoreCard.Location = new System.Drawing.Point(368, 12);
            this.scoreCard.Name = "scoreCard";
            this.scoreCard.Size = new System.Drawing.Size(132, 388);
            this.scoreCard.TabIndex = 3;
            //
            // scoreCaption
            //
            this.scoreCaption.AutoSize = true;
            this.scoreCaption.BackColor = System.Drawing.Color.Transparent;
            this.scoreCaption.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 8.5F);
            this.scoreCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(130)))), ((int)(((byte)(140)))));
            this.scoreCaption.Location = new System.Drawing.Point(14, 14);
            this.scoreCaption.Name = "scoreCaption";
            this.scoreCaption.Size = new System.Drawing.Size(30, 16);
            this.scoreCaption.TabIndex = 0;
            this.scoreCaption.Text = owlgame.Strings.ScoreCaption;
            //
            // scoreValue
            //
            this.scoreValue.AutoSize = true;
            this.scoreValue.BackColor = System.Drawing.Color.Transparent;
            this.scoreValue.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 22F, System.Drawing.FontStyle.Bold);
            this.scoreValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.scoreValue.Location = new System.Drawing.Point(11, 34);
            this.scoreValue.Name = "scoreValue";
            this.scoreValue.Size = new System.Drawing.Size(36, 40);
            this.scoreValue.TabIndex = 1;
            this.scoreValue.Text = "0";
            //
            // levelCaption
            //
            this.levelCaption.AutoSize = true;
            this.levelCaption.BackColor = System.Drawing.Color.Transparent;
            this.levelCaption.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 8.5F);
            this.levelCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(130)))), ((int)(((byte)(140)))));
            this.levelCaption.Location = new System.Drawing.Point(14, 128);
            this.levelCaption.Name = "levelCaption";
            this.levelCaption.Size = new System.Drawing.Size(30, 16);
            this.levelCaption.TabIndex = 2;
            this.levelCaption.Text = owlgame.Strings.LevelCaption;
            //
            // levelValue
            //
            this.levelValue.AutoSize = true;
            this.levelValue.BackColor = System.Drawing.Color.Transparent;
            this.levelValue.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 22F, System.Drawing.FontStyle.Bold);
            this.levelValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.levelValue.Location = new System.Drawing.Point(11, 148);
            this.levelValue.Name = "levelValue";
            this.levelValue.Size = new System.Drawing.Size(36, 40);
            this.levelValue.TabIndex = 3;
            this.levelValue.Text = "1";
            //
            // linesCaption
            //
            this.linesCaption.AutoSize = true;
            this.linesCaption.BackColor = System.Drawing.Color.Transparent;
            this.linesCaption.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 8.5F);
            this.linesCaption.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(130)))), ((int)(((byte)(130)))), ((int)(((byte)(140)))));
            this.linesCaption.Location = new System.Drawing.Point(14, 242);
            this.linesCaption.Name = "linesCaption";
            this.linesCaption.Size = new System.Drawing.Size(30, 16);
            this.linesCaption.TabIndex = 4;
            this.linesCaption.Text = owlgame.Strings.LinesCaption;
            //
            // linesValue
            //
            this.linesValue.AutoSize = true;
            this.linesValue.BackColor = System.Drawing.Color.Transparent;
            this.linesValue.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 22F, System.Drawing.FontStyle.Bold);
            this.linesValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(46)))));
            this.linesValue.Location = new System.Drawing.Point(11, 262);
            this.linesValue.Name = "linesValue";
            this.linesValue.Size = new System.Drawing.Size(36, 40);
            this.linesValue.TabIndex = 5;
            this.linesValue.Text = "0";
            //
            // gametimer
            //
            this.gametimer.Interval = 1000;
            this.gametimer.Tick += new System.EventHandler(this.gametimer_Tick);
            //
            // flashTimer
            //
            // Drives the clear animation: each tick fades the highlighted lines a
            // step, and after FlashSteps ticks the lines are removed.
            this.flashTimer.Interval = 45;
            this.flashTimer.Tick += new System.EventHandler(this.flashTimer_Tick);
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(243)))), ((int)(((byte)(246)))));
            this.ClientSize = new System.Drawing.Size(512, 412);
            this.Controls.Add(this.scoreCard);
            this.Controls.Add(this.controlCard);
            this.Controls.Add(this.previewCard);
            this.Controls.Add(this.gamewiew);
            this.Font = new System.Drawing.Font(owlgame.Strings.UiFont, 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = owlgame.Strings.MainTitle;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.gaizi.ResumeLayout(false);
            this.gamewiew.ResumeLayout(false);
            this.previewCard.ResumeLayout(false);
            this.previewCard.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.proimg)).EndInit();
            this.controlCard.ResumeLayout(false);
            this.controlCard.PerformLayout();
            this.scoreCard.ResumeLayout(false);
            this.scoreCard.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel gamewiew;
        private System.Windows.Forms.Panel gaizi;
        private System.Windows.Forms.Label welcomelabel;
        private System.Windows.Forms.Panel previewCard;
        private System.Windows.Forms.Label previewTitle;
        private System.Windows.Forms.PictureBox proimg;
        private System.Windows.Forms.Panel controlCard;
        private System.Windows.Forms.Label controlTitle;
        private System.Windows.Forms.LinkLabel myweb;
        private System.Windows.Forms.Button buttonexit;
        private System.Windows.Forms.Button buttonback;
        private System.Windows.Forms.Button buttonstart;
        private System.Windows.Forms.Button buttonset;
        private System.Windows.Forms.Panel scoreCard;
        private System.Windows.Forms.Label scoreCaption;
        private System.Windows.Forms.Label scoreValue;
        private System.Windows.Forms.Label levelCaption;
        private System.Windows.Forms.Label levelValue;
        private System.Windows.Forms.Label linesCaption;
        private System.Windows.Forms.Label linesValue;
        private System.Windows.Forms.Timer gametimer;
        private System.Windows.Forms.Timer flashTimer;
    }
}
