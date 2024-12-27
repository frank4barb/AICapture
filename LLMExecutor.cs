using LLama.Common;
using LLama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;

namespace AICapture
{
    /**************
   
        // modello LLama da usare
        private static LLMExecutor executor;

            //init LLM
            string modelPath = Context.Instance.GetString("#LLM_modelPath");
            executor = new LLMExecutor(modelPath, 0.01f, 2048, 0); //string answer = executor.Ask("come ti chiami?");

                //--Llama------
                executor.StartSession(aiPrompt + "\r\n", aiAssistant);
                executor.AskAsync(textBox.Text, textBox);               // scrivo la risposta nella textBox
                //------------

                executor.StopRespose();  //interrompe la scrittura della risposta

    ***************/
    public class LLMExecutor
    {
        private List<string> AntiPrompts = new List<string> { "User:" };
        private string ModelPath;
        private float Temperature;
        private int MaxTokens;
        private int GPULayers;

        private ModelParams Parameters;
        private LLamaWeights Model;
        private InteractiveExecutor Executor;

        private ChatSession session;
        private CancellationTokenSource? cts = null;


        public LLMExecutor(string modelPath, float temperature = 0.5f, int maxTokens = 2048, int gpuLayers = 0)  // 0.5, 2048, 0
        {
            ModelPath = modelPath;
            Temperature = temperature;
            MaxTokens = maxTokens;
            GPULayers = gpuLayers;

            Parameters = new ModelParams(modelPath)
            {
                ContextSize = 32768,
                GpuLayerCount = gpuLayers
            };
            Model = LLamaWeights.LoadFromFile(Parameters);
            Executor = new InteractiveExecutor(Model.CreateContext(Parameters));
        }

        public void StartSession(string prompt, string assistantMessage = "")
        {
            // Add chat histories as prompt to tell AI how to act.
            var chatHistory = new ChatHistory();
            if (prompt != "") chatHistory.AddMessage(AuthorRole.System, prompt);
            if (assistantMessage != "") chatHistory.AddMessage(AuthorRole.Assistant, assistantMessage);
            session = new(Executor, chatHistory);
        }

        public void StopRespose() {  if (cts != null) cts.Cancel(); } // Interrompi il ciclo


        public async Task<string> AskAsync(string userMessage, TextBox? resposeBox = null)
        {
            if (session == null) session = new(Executor, new ChatHistory());

            InferenceParams inferenceParams = new InferenceParams()
            {
                //Temperature = Temperature,
                MaxTokens = MaxTokens,
                AntiPrompts = AntiPrompts
            };

            //using var cts = new CancellationTokenSource();
            string result = ""; cts = new CancellationTokenSource();
            if (resposeBox != null) { resposeBox.Text = ""; resposeBox.ReadOnly = true; }
            try
            {
                await foreach (
                    var text
                    in session.ChatAsync(
                        new ChatHistory.Message(AuthorRole.User, userMessage),
                        inferenceParams, cts.Token))
                {
                    if (resposeBox != null) resposeBox.Text += text;
                    result += text;
                }
            }
            catch (OperationCanceledException) { /*skip*/ } //Il ciclo è stato interrotto.
            finally { cts.Dispose(); cts = null; }
            //--esclude terminatore dal testo restituito
            if (resposeBox != null) resposeBox.Text = cleanAntiPrompts(resposeBox.Text);
            result = cleanAntiPrompts(result);
            //--
            if (resposeBox != null) { resposeBox.ReadOnly = false; }
            return result;
        }
        private string cleanAntiPrompts(string text)
        {
            string? match = AntiPrompts.FirstOrDefault(s => text.EndsWith(s, StringComparison.OrdinalIgnoreCase));
            if (match != null) { text = text.Substring(0, text.Length - match.Length); } // Sottrae la parte finale corrispondente da searchString
            return text;
        }

        public string Ask(string userMessage, TextBox? resposeBox = null)
        {
            return AskAsync(userMessage).Result;
        }
    }
}
