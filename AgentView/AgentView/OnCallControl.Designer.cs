namespace AgentView
{
    partial class OnCallControl
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
            components = new System.ComponentModel.Container();
            timer_Call = new System.Windows.Forms.Timer(components);
            label_Timer = new Label();
            label_FromNumber = new Label();
            btn_EndCall = new CircleButton();
            btn_MuteMic = new CircleButton();
            SuspendLayout();
            // 
            // label_Timer
            // 
            label_Timer.Dock = DockStyle.Top;
            label_Timer.Font = new Font("Figtree", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label_Timer.Location = new Point(0, 68);
            label_Timer.Name = "label_Timer";
            label_Timer.Size = new Size(558, 30);
            label_Timer.TabIndex = 0;
            label_Timer.Text = "00:00";
            label_Timer.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label_FromNumber
            // 
            label_FromNumber.Dock = DockStyle.Top;
            label_FromNumber.Font = new Font("Figtree", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label_FromNumber.Location = new Point(0, 0);
            label_FromNumber.Name = "label_FromNumber";
            label_FromNumber.Padding = new Padding(0, 30, 0, 0);
            label_FromNumber.Size = new Size(558, 68);
            label_FromNumber.TabIndex = 1;
            label_FromNumber.Text = "label1";
            label_FromNumber.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btn_EndCall
            // 
            btn_EndCall.BackColor = Color.DarkRed;
            btn_EndCall.FlatAppearance.BorderSize = 0;
            btn_EndCall.FlatStyle = FlatStyle.Flat;
            btn_EndCall.Location = new Point(242, 268);
            btn_EndCall.Name = "btn_EndCall";
            btn_EndCall.Padding = new Padding(0, 10, 0, 0);
            btn_EndCall.Size = new Size(80, 80);
            btn_EndCall.TabIndex = 2;
            btn_EndCall.Text = "End Call";
            btn_EndCall.TextAlign = ContentAlignment.TopCenter;
            btn_EndCall.UseVisualStyleBackColor = false;
            btn_EndCall.Click += btn_EndCall_Click;
            // 
            // btn_MuteMic
            // 
            btn_MuteMic.BackColor = SystemColors.ButtonHighlight;
            btn_MuteMic.BackgroundImage = Properties.Resources.microphone_105;
            btn_MuteMic.BackgroundImageLayout = ImageLayout.Stretch;
            btn_MuteMic.FlatStyle = FlatStyle.Flat;
            btn_MuteMic.Location = new Point(328, 268);
            btn_MuteMic.Name = "btn_MuteMic";
            btn_MuteMic.Size = new Size(80, 80);
            btn_MuteMic.TabIndex = 3;
            btn_MuteMic.UseVisualStyleBackColor = false;
            btn_MuteMic.Click += btn_MuteMic_Click;
            // 
            // OnCallControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(btn_MuteMic);
            Controls.Add(btn_EndCall);
            Controls.Add(label_Timer);
            Controls.Add(label_FromNumber);
            Name = "OnCallControl";
            Size = new Size(558, 456);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Timer timer_Call;
        private Label label_Timer;
        private Label label_FromNumber;
        private CircleButton btn_EndCall;
        private CircleButton btn_MuteMic;
    }
}
