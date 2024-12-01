using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICapture
{
    // Classe di supporto per la finestra di selezione
    public class SelectionForm : Form
    {
        public Rectangle SelectionRectangle { get; private set; }

        public SelectionForm()
        {
            // Configura il form per la selezione
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.White;
            this.Opacity = 0.3;
            this.Cursor = Cursors.Cross;
            this.TopMost = true;
        }

        private Point start;
        private Rectangle selection;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            start = e.Location;
            selection = new Rectangle(e.Location, Size.Empty);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                selection = new Rectangle(
                    Math.Min(start.X, e.X),
                    Math.Min(start.Y, e.Y),
                    Math.Abs(start.X - e.X),
                    Math.Abs(start.Y - e.Y));
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            SelectionRectangle = selection;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void InitializeComponent()
        {

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (Brush brush = new SolidBrush(Color.FromArgb(128, Color.Blue)))
            {
                e.Graphics.FillRectangle(brush, selection);
            }
            base.OnPaint(e);
        }
    }
}
