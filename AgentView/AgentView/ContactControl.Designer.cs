namespace AgentView
{
    partial class ContactControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btn_Delete = new Button();
            btn_Edit = new Button();
            lb_ContactName = new Label();
            btn_Info = new Button();
            btn_Call = new Button();
            SuspendLayout();
            // 
            // btn_Delete
            // 
            btn_Delete.BackColor = Color.FromArgb(255, 86, 91);
            btn_Delete.BackgroundImage = Properties.Resources.trash;
            btn_Delete.BackgroundImageLayout = ImageLayout.Center;
            btn_Delete.Dock = DockStyle.Right;
            btn_Delete.FlatAppearance.BorderSize = 0;
            btn_Delete.FlatStyle = FlatStyle.Flat;
            btn_Delete.Location = new Point(590, 0);
            btn_Delete.Name = "btn_Delete";
            btn_Delete.Size = new Size(55, 62);
            btn_Delete.TabIndex = 0;
            btn_Delete.UseVisualStyleBackColor = false;
            btn_Delete.Click += btn_Delete_Click;
            // 
            // btn_Edit
            // 
            btn_Edit.BackColor = Color.FromArgb(0, 125, 217);
            btn_Edit.BackgroundImage = Properties.Resources.edit;
            btn_Edit.BackgroundImageLayout = ImageLayout.Center;
            btn_Edit.Dock = DockStyle.Right;
            btn_Edit.FlatAppearance.BorderSize = 0;
            btn_Edit.FlatStyle = FlatStyle.Flat;
            btn_Edit.Location = new Point(535, 0);
            btn_Edit.Name = "btn_Edit";
            btn_Edit.Size = new Size(55, 62);
            btn_Edit.TabIndex = 1;
            btn_Edit.UseVisualStyleBackColor = false;
            btn_Edit.Click += btn_Edit_Click;
            // 
            // lb_ContactName
            // 
            lb_ContactName.Anchor = AnchorStyles.Left;
            lb_ContactName.AutoSize = true;
            lb_ContactName.Font = new Font("Figtree", 11.9999981F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lb_ContactName.Location = new Point(16, 20);
            lb_ContactName.Name = "lb_ContactName";
            lb_ContactName.Size = new Size(50, 20);
            lb_ContactName.TabIndex = 2;
            lb_ContactName.Text = "label1";
            // 
            // btn_Info
            // 
            btn_Info.BackgroundImage = Properties.Resources.info;
            btn_Info.BackgroundImageLayout = ImageLayout.Center;
            btn_Info.Dock = DockStyle.Right;
            btn_Info.FlatAppearance.BorderSize = 0;
            btn_Info.FlatStyle = FlatStyle.Flat;
            btn_Info.Location = new Point(425, 0);
            btn_Info.Name = "btn_Info";
            btn_Info.Size = new Size(55, 62);
            btn_Info.TabIndex = 3;
            btn_Info.UseVisualStyleBackColor = true;
            btn_Info.Click += btn_Info_Click;
            // 
            // btn_Call
            // 
            btn_Call.BackColor = Color.SpringGreen;
            btn_Call.BackgroundImage = Properties.Resources.telephone_174;
            btn_Call.BackgroundImageLayout = ImageLayout.Zoom;
            btn_Call.Dock = DockStyle.Right;
            btn_Call.FlatAppearance.BorderSize = 0;
            btn_Call.FlatStyle = FlatStyle.Flat;
            btn_Call.Location = new Point(480, 0);
            btn_Call.Name = "btn_Call";
            btn_Call.Size = new Size(55, 62);
            btn_Call.TabIndex = 4;
            btn_Call.UseVisualStyleBackColor = false;
            btn_Call.Click += btn_Call_Click;
            // 
            // ContactControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(btn_Info);
            Controls.Add(btn_Call);
            Controls.Add(lb_ContactName);
            Controls.Add(btn_Edit);
            Controls.Add(btn_Delete);
            Name = "ContactControl";
            Size = new Size(645, 62);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btn_Delete;
        private Button btn_Edit;
        private Label lb_ContactName;
        private Button btn_Info;
        private Button btn_Call;
    }
}
