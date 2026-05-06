namespace AgentView
{
    partial class ContactForm
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
            btn_SaveContact = new Button();
            tb_FirstName = new TextBox();
            tb_LastName = new TextBox();
            tb_Email = new TextBox();
            lb_FirstName = new Label();
            lb_LastName = new Label();
            lb_Email = new Label();
            SuspendLayout();
            // 
            // btn_SaveContact
            // 
            btn_SaveContact.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btn_SaveContact.BackColor = SystemColors.ButtonHighlight;
            btn_SaveContact.FlatStyle = FlatStyle.Flat;
            btn_SaveContact.Font = new Font("Figtree", 9.749999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btn_SaveContact.Location = new Point(125, 206);
            btn_SaveContact.Name = "btn_SaveContact";
            btn_SaveContact.Size = new Size(87, 43);
            btn_SaveContact.TabIndex = 0;
            btn_SaveContact.Text = "Save Contact";
            btn_SaveContact.UseVisualStyleBackColor = false;
            btn_SaveContact.Click += btn_SaveContact_Click;
            // 
            // tb_FirstName
            // 
            tb_FirstName.Location = new Point(80, 33);
            tb_FirstName.Name = "tb_FirstName";
            tb_FirstName.Size = new Size(200, 23);
            tb_FirstName.TabIndex = 1;
            // 
            // tb_LastName
            // 
            tb_LastName.Location = new Point(80, 62);
            tb_LastName.Name = "tb_LastName";
            tb_LastName.Size = new Size(200, 23);
            tb_LastName.TabIndex = 2;
            // 
            // tb_Email
            // 
            tb_Email.Location = new Point(80, 93);
            tb_Email.Name = "tb_Email";
            tb_Email.Size = new Size(200, 23);
            tb_Email.TabIndex = 3;
            // 
            // lb_FirstName
            // 
            lb_FirstName.AutoSize = true;
            lb_FirstName.Font = new Font("Figtree", 9.749999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lb_FirstName.Location = new Point(5, 35);
            lb_FirstName.Name = "lb_FirstName";
            lb_FirstName.Size = new Size(73, 16);
            lb_FirstName.TabIndex = 4;
            lb_FirstName.Text = "First Name:";
            // 
            // lb_LastName
            // 
            lb_LastName.AutoSize = true;
            lb_LastName.Font = new Font("Figtree", 9.749999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lb_LastName.Location = new Point(5, 64);
            lb_LastName.Name = "lb_LastName";
            lb_LastName.Size = new Size(73, 16);
            lb_LastName.TabIndex = 5;
            lb_LastName.Text = "Last Name:";
            // 
            // lb_Email
            // 
            lb_Email.AutoSize = true;
            lb_Email.Font = new Font("Figtree", 9.749999F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lb_Email.Location = new Point(32, 95);
            lb_Email.Name = "lb_Email";
            lb_Email.Size = new Size(42, 16);
            lb_Email.TabIndex = 6;
            lb_Email.Text = "Email:";
            // 
            // ContactForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(242, 246, 248);
            ClientSize = new Size(334, 261);
            Controls.Add(lb_Email);
            Controls.Add(lb_LastName);
            Controls.Add(lb_FirstName);
            Controls.Add(tb_Email);
            Controls.Add(tb_LastName);
            Controls.Add(tb_FirstName);
            Controls.Add(btn_SaveContact);
            Name = "ContactForm";
            Text = "Contact Information";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btn_SaveContact;
        private TextBox tb_FirstName;
        private TextBox tb_LastName;
        private TextBox tb_Email;
        private Label lb_FirstName;
        private Label lb_LastName;
        private Label lb_Email;
    }
}