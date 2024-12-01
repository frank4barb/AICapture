using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Tesseract;
using static System.Windows.Forms.DataFormats;




namespace AICapture
{
    internal class CaptureHelper
    {
        public static void CaptureAreaAndProcess(Action<Bitmap> processBitmap)
        {
            // Cattura l'area selezionata e salva l'immagine
            Rectangle selection = CaptureSelection();

            if (selection != Rectangle.Empty)
            {
                // Applica il fattore di scala calcolato con DPI alla selezione
                using (var bmp = new Bitmap(selection.Width, selection.Height))
                {
                    bmp.SetResolution(96, 96);  // Utilizza la risoluzione dello schermo per evitare distorsioni nei DPI
                    using (var gBmp = Graphics.FromImage(bmp))
                    {
                        gBmp.CopyFromScreen(
                            selection.X,
                            selection.Y,
                            0, 0,
                            bmp.Size
                        );
                    }
                    processBitmap(bmp);
                }
            }
        }

        // Restituisce bitmap dello schermo
        public static Bitmap CaptureFullScreen()
        {
            // Calcola i confini totali dell'area dello schermo considerando tutti i monitor
            int screenWidth = 0;
            int screenHeight = 0;

            //foreach (var screen in Screen.AllScreens)
            //{
            //    screenWidth = Math.Max(screenWidth, screen.Bounds.Right);
            //    screenHeight = Math.Max(screenHeight, screen.Bounds.Bottom);
            //}

            // Ottieni lo schermo su cui si trova il cursore del mouse
            Screen currentScreen = Screen.FromPoint(Cursor.Position);

            // Restituisci i confini di questo schermo
            Rectangle scrn = currentScreen.Bounds;

            screenWidth = scrn.Width;
            screenHeight = scrn.Height;

            // Crea un bitmap della dimensione totale dello schermo
            Bitmap bitmap = new Bitmap(screenWidth, screenHeight);

            // Utilizza la risoluzione dello schermo per evitare distorsioni nei DPI
            bitmap.SetResolution(96, 96);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // Usa CopyFromScreen per catturare l'intero contenuto visibile su tutti i monitor
                g.CopyFromScreen(0, 0, 0, 0, new Size(screenWidth, screenHeight));
            }

            return bitmap;
        }

 
        // Apre una finestra di selezione usando il mouse
        private static Rectangle CaptureSelection()
        {
            
            using (var form = new SelectionForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    return form.SelectionRectangle;
                    //return AdjustForScreen(form.SelectionRectangle);
                    //return AdjustForScreen(ScaleRectangleForDpi(form.SelectionRectangle));
                    //return ManualAdjustForScreen(ScaleRectangleForDpi(form.SelectionRectangle));
                    //return ManualAdjustForScreen(form.SelectionRectangle);
                }
            }
            return Rectangle.Empty;
        }
        private static Rectangle ScaleRectangleForDpi(Rectangle rect)
        {
            // Ottieni la scala DPI
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = g.DpiX;
                float dpiY = g.DpiY;

                // La scala di default è 96 DPI
                float scaleX = (dpiX / 96.0f) + 1;
                float scaleY = (dpiY / 96.0f) + 1;

                return new Rectangle(
                    (int)(rect.X * scaleX),
                    (int)(rect.Y * scaleY),
                    (int)(rect.Width * scaleX),
                    (int)(rect.Height * scaleY));
            }
        }
        private static Rectangle AdjustForScreen(Rectangle selection)
        {
            // Trova lo schermo che contiene il punto di partenza della selezione
            var screen = Screen.FromPoint(selection.Location);

            // Calcola le coordinate relative per la selezione sullo schermo corretto
            Rectangle adjustedRectangle = new Rectangle(
                selection.X - screen.Bounds.X,
                selection.Y - screen.Bounds.Y,
                selection.Width,
                selection.Height);

            return adjustedRectangle;
        }
        private static Rectangle ManualAdjustForScreen(Rectangle selection)
        {

                // Calcola le coordinate relative per la selezione sullo schermo corretto
                Rectangle adjustedRectangle = new Rectangle(
                    selection.X + 55,
                    selection.Y - 166,
                    selection.Width,
                    selection.Height);

                return adjustedRectangle;
        }


    }
}
