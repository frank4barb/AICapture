
using LLama.Common;
using LLama;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using TesseractPDF;
using System.CodeDom.Compiler;

namespace AICapture
{
    class Program
    {
        // modello LLama da usare
        private static LLMExecutor executor;
        //menù su icona in barra delle applicazioni windows
        private static NotifyIcon notifyIcon;
        // Lista per memorizzare i testi mostrati nella finestra "Testo Estratto"
        private static List<string> textHistory;

        [STAThread]
        static async Task Main()
        {
            //init LLM
            string modelPath = Context.Instance.GetString("#LLM_modelPath");
            executor = new LLMExecutor(modelPath, 0.01f, 2048, 0); //string answer = executor.Ask("come ti chiami?");

            //init App
            textHistory = new List<string>();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Crea e configura l'icona nella barra delle applicazioni
            notifyIcon = new NotifyIcon
            {
                Icon = new Icon("Capture.ico"),   //SystemIcons.Application,
                Visible = true,
                Text = "AICapture",
                ContextMenuStrip = new ContextMenuStrip()
            };
            notifyIcon.Click += (s, e) =>
            {
                if ( ((MouseEventArgs)e).Button == MouseButtons.Left) { ShowTextForm(""); }
            };

            // Voci di menu
            notifyIcon.ContextMenuStrip.Items.Add("Cattura Immagine", null, CaptureImage);
            notifyIcon.ContextMenuStrip.Items.Add("Cattura Testo", null, CaptureText);
            notifyIcon.ContextMenuStrip.Items.Add("Cattura Testo+", null, CaptureTextPlus);
            notifyIcon.ContextMenuStrip.Items.Add("Elabora Testo", null, ShowText);
            notifyIcon.ContextMenuStrip.Items.Add("Esci", null, (s, e) => Application.Exit());

            Application.Run();
        }

        static void CaptureImage(object sender, EventArgs e)
        {
            CaptureHelper.CaptureAreaAndProcess((bmp) =>
            {
                // Inserisco negli appunti in un nuovo Thread STA ie: Thread.CurrentThread.GetApartmentState() == ApartmentState.STA
                // Posso mettere negli appunti solo da un thread STA
                Thread staThread = new Thread(() =>
                {
                    Clipboard.SetImage(bmp);
                    MessageBox.Show("Immagine copiata negli appunti!");
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            });
        }
        static void CaptureText(object sender, EventArgs e)
        {
            CaptureHelper.CaptureAreaAndProcess((bmp) =>
            {
                string extractedText = TesseractHelper.ExtractTextFromBmp(bmp); // Funzione per estrarre testo

                // Inserisco negli appunti in un nuovo Thread STA ie: Thread.CurrentThread.GetApartmentState() == ApartmentState.STA
                // Posso mettere negli appunti solo da un thread STA
                Thread staThread = new Thread(() =>
                {
                    Clipboard.SetText(extractedText);
                    MessageBox.Show("Testo copiato negli appunti!");
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            });
        }
        static void CaptureTextPlus(object sender, EventArgs e)
        {
            CaptureHelper.CaptureAreaAndProcess((bmp) =>
            {
                string extractedText = TesseractHelper.ExtractTextFromBmp(bmp); // Funzione per estrarre testo

                // Inserisco negli appunti in un nuovo Thread STA ie: Thread.CurrentThread.GetApartmentState() == ApartmentState.STA
                // Posso mettere negli appunti solo da un thread STA
                Thread staThread = new Thread(() =>
                {
                    Clipboard.SetText(Clipboard.GetText() + Environment.NewLine + extractedText);
                    MessageBox.Show("Testo aggiunto negli appunti!");
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            });
        }
        static void ShowText(object sender, EventArgs e)
        {
            CaptureHelper.CaptureAreaAndProcess((bmp) =>
            {
                string extractedText = TesseractHelper.ExtractTextFromBmp(bmp); // Funzione per estrarre testo
                CloseTextForm();
                ShowTextForm(extractedText); //visualizza il testo estratto nella form
            });
        }


        //------------------------------------------------------------------------------------------------------
        // GESTIONE FORM

        private static Form mainForm;

        static void CloseTextForm()
        {
            if (mainForm != null && mainForm.IsDisposed == false) { mainForm.Close(); mainForm.Dispose(); }
        }
        static void ShowTextForm(string extractedText)
        {
            // se la finestra già esiste ritorno alla finestra aperta
            if (mainForm != null && mainForm.IsDisposed == false) 
            {
                if (!mainForm.Visible) { mainForm.ShowDialog(); }
                else if (mainForm.WindowState == FormWindowState.Minimized) { mainForm.WindowState = FormWindowState.Normal; }
                else { mainForm.Focus(); } // Porta la finestra esistente in primo piano
                return;
            }


            // creo nuova finestra
            mainForm = new Form
            {
                Text = "Testo Estratto",
                Icon = new Icon("Capture.ico"),
                StartPosition = FormStartPosition.CenterScreen,
                Width = 500,
                Height = 300
            };


            TextBox textBox = new TextBox
            {
                Text = extractedText, SelectionStart = extractedText.Length, SelectionLength = 0,
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Both,
                WordWrap = false  // a capo automatico
            };
            TextBox questionBox = new TextBox
            {
                Text = "",
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.None,
                WordWrap = true,  // a capo automatico
                ReadOnly = true
            };

            //Domanda
            Button questionButton = new Button
            {
                Text = "Domanda",
                Width = 85, // Imposta la larghezza desiderata
                Dock = DockStyle.Left,
                Enabled = false
            };
            questionButton.Click += (s, e) =>
            {
                //--Llama------
                executor.AskAsync(textBox.Text, textBox);                   // scrivo la risposta nella textBox
                //------------
            };
            textBox.ReadOnlyChanged += (s, e) =>
            {
                if (textBox.ReadOnly) {
                    questionBox.ReadOnly = true;
                    questionButton.Enabled = false; 
                }
                else {
                    textHistory.Add(questionBox.Text);    // salvo la domanda
                    textHistory.Add(textBox.Text);        // Aggiungi il testo alla cronologia
                    questionBox.Text = "";                  // cancello la Box della domanda
                    questionBox.ReadOnly = false;
                    questionButton.Enabled = true;
                }
            };
            Button stopButton = new Button
            {
                Text = "Stop",
                Width = 40, // Imposta la larghezza desiderata
                Dock = DockStyle.Left,
            };
            stopButton.Click += (s, e) =>
            {
                executor.StopRespose();  //interrompe la scrittura della risposta
            };
            //Conversazione
            Button conversationButton = new Button
            {
                Text = "Conversazione",
                Width = 130, // Imposta la larghezza desiderata
                Dock = DockStyle.Right
            };
            conversationButton.Click += (s, e) =>
            {
                using (Form historyForm = new Form())
                {
                    historyForm.Text = "Cronologia";
                    historyForm.Width = 400;
                    historyForm.Height = 300;

                    string hisText = string.Join(Environment.NewLine + "--------------" + Environment.NewLine, textHistory);

                    TextBox historyBox = new TextBox
                    {
                        Multiline = true,
                        ReadOnly = true,
                        Dock = DockStyle.Fill,
                        ScrollBars = ScrollBars.Both,
                        Text = hisText, 
                        SelectionStart = hisText.Length,
                        SelectionLength = 0
                    };

                    historyForm.Controls.Add(historyBox);
                    historyForm.ShowDialog();
                }
            };



            //---------------------------------------------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------------------------------------------
            //Traduci It->En
            //Button traduciItEnButton = new Button
            //{
            //    Text = "Traduci: It->En",
            //    Width = 130, // Imposta la larghezza desiderata
            //    Dock = DockStyle.Bottom
            //};
            //traduciItEnButton.Click += (s, e) =>
            //{
            //    // Implementa la logica di salvataggio
            //    MessageBox.Show("Traduci: It->En");

            //    ////--Llama------
            //    ////executor.StartSession("Sei un traduttore professionista. Chiedi all'utente di sottoporti un testo in italiano, e tu lo tradurrai in inglese. Se non riscontri un testo traducibile spiega il motivo per cui non puoi effettuare la traduzione.", "Puoi inserire il testo in italiano da tradurre in inglese?");
            //    //executor.StartSession("Puoi tradurre in inglese il seguente testo?");
            //    //executor.AskAsync(textBox.Text, textBox);
            //    //questionButton.Enabled = true;
            //    ////------------
            //    //--Llama------
            //    executor.StartSession("Sei un traduttore professionista. Puoi solo tradurre il testo che ti viene passato dall'utente senza aggiungere altri commenti.");
            //    executor.AskAsync("Puoi tradurre in inglese il seguente testo? " + textBox.Text, textBox);
            //    questionButton.Enabled = true;

            //};
            ////Traduci En->It
            //Button traduciEnItButton = new Button
            //{
            //    Text = "Traduci: En->It",
            //    Width = 130, // Imposta la larghezza desiderata
            //    Dock = DockStyle.Bottom
            //};
            //traduciEnItButton.Click += (s, e) =>
            //{
            //    // Implementa la logica di salvataggio
            //    MessageBox.Show("Traduci: En->It");

            //    //--Llama------
            //    executor.StartSession("Sei un traduttore professionista. Chiedi all'utente di sottoporti un testo in inglese, e tu lo tradurrai in italiano. Se non riscontri un testo traducibile spiega il motivo per cui non puoi effettuare la traduzione.", "Puoi inserire il testo in inglese da tradurre in italiano?");
            //    executor.AskAsync(textBox.Text, textBox);
            //    questionButton.Enabled = true;
            //    //------------

            //};
            ////Salva (non implementato)
            //Button saveButton = new Button
            //{
            //    Text = "Salva",
            //    Width = 130, // Imposta la larghezza desiderata
            //    Dock = DockStyle.Bottom
            //};
            //saveButton.Click += (s, e) =>
            //{
            //    // Implementa la logica di salvataggio
            //    MessageBox.Show("Testo salvato!");

            //    //--Llama------
            //    executor.StartSession("");
            //    executor.AskAsync("come ti chiami?", textBox);
            //    ////------------

            //};
            //---------------------------------------------------------------------------------------------------------------------------------
            //---------------------------------------------------------------------------------------------------------------------------------


            //Chiudi
            Button closeButton = new Button
            {
                Text = "Chiudi",
                Width = 130, // Imposta la larghezza desiderata
                Dock = DockStyle.Bottom
            };
            closeButton.Click += (s, e) => 
            {
                textHistory.Clear();    
                mainForm.Close();
            };

            // Creazione del TableLayoutPanel
            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.Dock = DockStyle.Fill;
            // Configurazione delle colonne
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 90));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            // Aggiunta del FlowLayoutPanel per i pulsanti
            FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel();
            flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            flowLayoutPanel.WrapContents = true;
            flowLayoutPanel.AutoScroll = true;
            flowLayoutPanel.Dock = DockStyle.Fill;
            // Aggiunta dei pulsanti al FlowLayoutPanel
            //flowLayoutPanel.Controls.Add(questionButton);
            flowLayoutPanel.Controls.Add(conversationButton);
            //flowLayoutPanel.Controls.Add(traduciItEnButton);
            //flowLayoutPanel.Controls.Add(traduciEnItButton);
            //flowLayoutPanel.Controls.Add(saveButton);

            //ciclo bottoni da file ini
            for(int  i = 1; i <= 10; i++)
            {
                string ctxPrompt = Context.Instance.GetString($"#LLM_appPrompt{i}");
                if (ctxPrompt != "") flowLayoutPanel.Controls.Add(AddAIButton(ctxPrompt, textBox));
            }
            flowLayoutPanel.Controls.Add(closeButton);


            // Aggiunta del TableLayoutPanel per i pulsanti
            TableLayoutPanel tableLayoutButton = new TableLayoutPanel();
            tableLayoutButton.ColumnCount = 2;
            tableLayoutButton.RowCount = 1;
            tableLayoutButton.Dock = DockStyle.Fill;
            tableLayoutButton.Controls.Add(questionButton, 0, 0);
            tableLayoutButton.Controls.Add(stopButton, 1, 0);

            // Aggiunta del controllo per il testo e i pulsanti
            tableLayoutPanel.Controls.Add(textBox, 0, 0);
            tableLayoutPanel.Controls.Add(flowLayoutPanel, 1, 0);
            tableLayoutPanel.Controls.Add(questionBox, 0, 1);
            tableLayoutPanel.Controls.Add(tableLayoutButton, 1, 1);
            mainForm.Controls.Add(tableLayoutPanel);

            mainForm.ShowDialog();
        }
        private static Button AddAIButton(string aiConditions, TextBox textBox)
        {
            int nCond = aiConditions.Split('|').Length;
            string buttonName = aiConditions.Split('|')[0];
            string aiPrompt = (nCond > 1) ? aiConditions.Split('|')[1] : "";
            string aiAssistant = (nCond > 2) ? aiConditions.Split('|')[2] : "";

            Button condButton = new Button
            {
                Text = buttonName,
                Width = 130, // Imposta la larghezza desiderata
                Dock = DockStyle.Bottom
            };
            condButton.Click += (s, e) =>
            {
                textHistory.Add(textBox.Text);  // Aggiungi il testo alla cronologia
                //--Llama------
                executor.StartSession(aiPrompt + "\r\n", aiAssistant);
                executor.AskAsync(textBox.Text, textBox);
                //------------
            };
            return condButton;
        }

    }



}