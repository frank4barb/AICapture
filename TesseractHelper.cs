using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;
using TesseractPDF;

namespace AICapture
{
    internal class TesseractHelper
    {

        public static string ExtractTextFromBmp(Bitmap bmp)
        {
            string extractedText = "";

            // Forza DefaultDir in base a dove viene avviato il programma
            //string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dump.log");

            //string tessdataDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "/tessdata"));
            string tessdataDir = AppDomain.CurrentDomain.BaseDirectory + "/tessdata";
            //if (!Directory.Exists(tessdataDir)) { tessdataDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../tessdata")); } //prova dir di debug
            if (!Directory.Exists(tessdataDir)) { MessageBox.Show("Tessdata non trovato: " + tessdataDir); }

            //MessageBox.Show(tessdataDir);

            // Inizializza TesseractEngine una sola volta
            using (var engine = new TesseractEngine(tessdataDir, "ita", EngineMode.Default))
            {
                // Converti l'immagine Bitmap in un array di byte[] PNG
                using (var memoryStream = new MemoryStream())
                {
                    bmp.Save((Stream)memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                    //if (applicaFiltro) outputMemoryStream = filtroNitidezza(memoryStream);
                    memoryStream.Position = 0; // Resetta la posizione del MemoryStream

                    //---------------------------------------------------------------
                    //TESSERACT
                    //---------------------------------------------------------------
                    //Elenco dei PSM
                    //0: Solo rilevamento dell’orientamento e dello script(OSD).
                    //1: Segmentazione automatica della pagina con OSD.
                    //2: Segmentazione automatica, ma senza OSD o OCR(non implementato).
                    //3: Segmentazione automatica completa, ma senza OSD(predefinito).
                    //4: Assume una singola colonna di testo di dimensioni variabili.
                    //5: Assume un blocco uniforme di testo allineato verticalmente.
                    //6: Assume un blocco uniforme di testo.
                    //7: Tratta l’immagine come una singola linea di testo.
                    //8: Tratta l’immagine come una singola parola.
                    //9: Tratta l’immagine come una singola parola in un cerchio.
                    //10: Tratta l’immagine come un singolo carattere.
                    //11: Testo sparso. Trova il maggior numero possibile di testi senza un ordine particolare.
                    //12: Testo sparso con OSD.
                    //13: Linea grezza. Tratta l’immagine come una singola linea di testo, bypassando hack specifici di Tesseract.
                    //
                    using (var imgPix = Pix.LoadFromMemory(memoryStream.ToArray()))
                    using (var page = engine.Process(imgPix, " --psm 6"))  //using (var page = engine.Process(imgPix, PageSegMode.SingleBlock))  //using (var page = engine.Process(imgPix))
                    {
                        // get Image text All
                        //extractedText += page.GetText();

                        // get Image text lines
                        List<string> lines = new List<string>(); 
                        using (var iter = page.GetIterator())
                        {
                            iter.Begin();
                            do
                            {
                                if (iter.IsAtBeginningOf(PageIteratorLevel.TextLine))
                                {
                                    string line = iter.GetText(PageIteratorLevel.TextLine); lines.Add(line); 
                                }
                            } while (iter.Next(PageIteratorLevel.TextLine));
                        }
                        //extract Text
                        extractedText += extractText(lines); 
                    }
                    //-----------------------------------------------------------------
                }
            }
            return extractedText;
        }
        private static string extractText(List<string> lines)
        {
            if (lines.Count() == 0) { lines.Add("?"); }  
            int startLine = 0, endLine = lines.Count() - 1; 
            while (lines[startLine].TrimEnd(new char[] { ' ', '\t', '\n' }) == "") startLine++;  // scarto prime linee vuote
            while (lines[endLine].TrimEnd(new char[] { ' ', '\t', '\n' }) == "") endLine--; // scarto ultime linee vuote
            //---
            string extractedText = string.Join("", lines.GetRange(startLine, endLine - startLine + 1)).TrimEnd('\n'); 
            extractedText = extractedText.Replace("\r\n", "\n").Replace("\n", "\r\n"); //converte con "a capo" per windows
            return extractedText;
        }
    }
}
