namespace AgentView
{
    partial class MainView
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lb_Status = new Label();
            comboBox1 = new ComboBox();
            PanelIncomingCalls = new Panel();
            btn_Home = new Button();
            btn_History = new Button();
            panel_Menu = new Panel();
            PanelActiveCall = new Panel();
            panel_Top = new Panel();
            panel_Logo = new Panel();
            pictureBox1 = new PictureBox();
            panel1 = new Panel();
            panel_Menu.SuspendLayout();
            panel_Top.SuspendLayout();
            panel_Logo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // lb_Status
            // 
            lb_Status.AutoSize = true;
            lb_Status.Dock = DockStyle.Right;
            lb_Status.Font = new Font("Segoe UI", 12F);
            lb_Status.Location = new Point(549, 0);
            lb_Status.Name = "lb_Status";
            lb_Status.Padding = new Padding(0, 5, 130, 0);
            lb_Status.Size = new Size(189, 26);
            lb_Status.TabIndex = 0;
            lb_Status.Text = "Status: ";
            lb_Status.Click += lb_Status_Click;
            // 
            // comboBox1
            // 
            comboBox1.Anchor = AnchorStyles.Right;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(607, 6);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(121, 23);
            comboBox1.TabIndex = 1;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // PanelIncomingCalls
            // 
            PanelIncomingCalls.AutoScroll = true;
            PanelIncomingCalls.BackColor = Color.FromArgb(242, 246, 248);
            PanelIncomingCalls.Dock = DockStyle.Fill;
            PanelIncomingCalls.Location = new Point(155, 66);
            PanelIncomingCalls.Name = "PanelIncomingCalls";
            PanelIncomingCalls.Padding = new Padding(10);
            PanelIncomingCalls.Size = new Size(738, 444);
            PanelIncomingCalls.TabIndex = 2;
            PanelIncomingCalls.Paint += PanelIncomingCalls_Paint;
            // 
            // btn_Home
            // 
            btn_Home.Dock = DockStyle.Top;
            btn_Home.FlatAppearance.BorderSize = 0;
            btn_Home.FlatStyle = FlatStyle.Flat;
            btn_Home.Font = new Font("Figtree Medium", 10F, FontStyle.Bold);
            btn_Home.ForeColor = Color.FromArgb(0, 125, 217);
            btn_Home.Image = Properties.Resources.home1;
            btn_Home.ImageAlign = ContentAlignment.MiddleLeft;
            btn_Home.Location = new Point(0, 0);
            btn_Home.Name = "btn_Home";
            btn_Home.Size = new Size(155, 40);
            btn_Home.TabIndex = 3;
            btn_Home.Text = "  Home";
            btn_Home.TextImageRelation = TextImageRelation.ImageBeforeText;
            btn_Home.UseVisualStyleBackColor = true;
            btn_Home.Click += btn_Home_Click;
            // 
            // btn_History
            // 
            btn_History.Dock = DockStyle.Top;
            btn_History.FlatAppearance.BorderSize = 0;
            btn_History.FlatStyle = FlatStyle.Flat;
            btn_History.Font = new Font("Figtree Medium", 10F, FontStyle.Bold);
            btn_History.ForeColor = Color.FromArgb(0, 125, 217);
            btn_History.Image = Properties.Resources.journal;
            btn_History.ImageAlign = ContentAlignment.MiddleLeft;
            btn_History.Location = new Point(0, 40);
            btn_History.Name = "btn_History";
            btn_History.Size = new Size(155, 40);
            btn_History.TabIndex = 4;
            btn_History.Text = "  Call History";
            btn_History.TextImageRelation = TextImageRelation.ImageBeforeText;
            btn_History.UseVisualStyleBackColor = true;
            btn_History.Click += btn_History_Click;
            // 
            // panel_Menu
            // 
            panel_Menu.BackColor = SystemColors.ControlLightLight;
            panel_Menu.Controls.Add(btn_History);
            panel_Menu.Controls.Add(btn_Home);
            panel_Menu.Dock = DockStyle.Left;
            panel_Menu.Location = new Point(0, 32);
            panel_Menu.Name = "panel_Menu";
            panel_Menu.Size = new Size(155, 478);
            panel_Menu.TabIndex = 6;
            // 
            // PanelActiveCall
            // 
            PanelActiveCall.BackColor = Color.FromArgb(242, 246, 248);
            PanelActiveCall.Dock = DockStyle.Fill;
            PanelActiveCall.Location = new Point(155, 66);
            PanelActiveCall.Name = "PanelActiveCall";
            PanelActiveCall.Size = new Size(738, 444);
            PanelActiveCall.TabIndex = 9;
            PanelActiveCall.Visible = false;
            // 
            // panel_Top
            // 
            panel_Top.BackColor = Color.FromArgb(3, 32, 74);
            panel_Top.Controls.Add(panel_Logo);
            panel_Top.Dock = DockStyle.Top;
            panel_Top.Location = new Point(0, 0);
            panel_Top.Name = "panel_Top";
            panel_Top.Size = new Size(893, 32);
            panel_Top.TabIndex = 7;
            // 
            // panel_Logo
            // 
            panel_Logo.Controls.Add(pictureBox1);
            panel_Logo.Dock = DockStyle.Left;
            panel_Logo.Location = new Point(0, 0);
            panel_Logo.Name = "panel_Logo";
            panel_Logo.Size = new Size(155, 32);
            panel_Logo.TabIndex = 0;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.upscalemedia_transformedtransparent2;
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(155, 32);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(253, 254, 255);
            panel1.Controls.Add(comboBox1);
            panel1.Controls.Add(lb_Status);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(155, 32);
            panel1.Name = "panel1";
            panel1.Size = new Size(738, 34);
            panel1.TabIndex = 8;
            // 
            // MainView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(242, 246, 248);
            ClientSize = new Size(893, 510);
            Controls.Add(PanelIncomingCalls);
            Controls.Add(PanelActiveCall);
            Controls.Add(panel1);
            Controls.Add(panel_Menu);
            Controls.Add(panel_Top);
            MinimumSize = new Size(909, 549);
            Name = "MainView";
            panel_Menu.ResumeLayout(false);
            panel_Top.ResumeLayout(false);
            panel_Logo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label lb_Status;
        private ComboBox comboBox1;
        private System.Windows.Forms.Panel PanelIncomingCalls;
        private Button btn_Home;
        private Button btn_History;
        private System.Windows.Forms.Panel panel_Menu;
        private System.Windows.Forms.Panel panel_Top;
        private System.Windows.Forms.Panel panel_Logo;
        private PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel PanelActiveCall;
    }
}
