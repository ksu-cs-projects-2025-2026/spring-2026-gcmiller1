namespace AgentView
{
    partial class Form1
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
            panel1 = new Panel();
            btn_Home = new Button();
            btn_History = new Button();
            Btn_SendToDTMF = new Button();
            SuspendLayout();
            // 
            // lb_Status
            // 
            lb_Status.AutoSize = true;
            lb_Status.Font = new Font("Segoe UI", 12F);
            lb_Status.Location = new Point(523, 18);
            lb_Status.Name = "lb_Status";
            lb_Status.Size = new Size(59, 21);
            lb_Status.TabIndex = 0;
            lb_Status.Text = "Status: ";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(577, 18);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(121, 23);
            comboBox1.TabIndex = 1;
            // 
            // panel1
            // 
            panel1.Location = new Point(161, 67);
            panel1.Name = "panel1";
            panel1.Size = new Size(537, 306);
            panel1.TabIndex = 2;
            // 
            // btn_Home
            // 
            btn_Home.Font = new Font("Segoe UI", 12F);
            btn_Home.Location = new Point(30, 90);
            btn_Home.Name = "btn_Home";
            btn_Home.Size = new Size(107, 40);
            btn_Home.TabIndex = 3;
            btn_Home.Text = "Home";
            btn_Home.UseVisualStyleBackColor = true;
            // 
            // btn_History
            // 
            btn_History.Font = new Font("Segoe UI", 12F);
            btn_History.Location = new Point(30, 140);
            btn_History.Name = "btn_History";
            btn_History.Size = new Size(107, 40);
            btn_History.TabIndex = 4;
            btn_History.Text = "Call History";
            btn_History.UseVisualStyleBackColor = true;
            // 
            // Btn_SendToDTMF
            // 
            Btn_SendToDTMF.Location = new Point(454, 397);
            Btn_SendToDTMF.Name = "Btn_SendToDTMF";
            Btn_SendToDTMF.Size = new Size(75, 23);
            Btn_SendToDTMF.TabIndex = 5;
            Btn_SendToDTMF.Text = "DTMF";
            Btn_SendToDTMF.UseVisualStyleBackColor = true;
            Btn_SendToDTMF.Click += Btn_SendToDTMF_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(750, 450);
            Controls.Add(Btn_SendToDTMF);
            Controls.Add(btn_History);
            Controls.Add(btn_Home);
            Controls.Add(panel1);
            Controls.Add(comboBox1);
            Controls.Add(lb_Status);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lb_Status;
        private ComboBox comboBox1;
        private Panel panel1;
        private Button btn_Home;
        private Button btn_History;
        private Button Btn_SendToDTMF;
    }
}
