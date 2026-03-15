using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AgentView
{
    public class CircleButton : Button
    {
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, this.Width, this.Height);
            this.Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillEllipse(brush, 0, 0, Width, Height);
            }

            base.OnPaint(e);

            using (Pen pen = new Pen(Color.Black, 2))
            {
                e.Graphics.DrawEllipse(pen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}
