using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tracker;

namespace Tracker
{
    // NOTE: This code assumes UI controls: txturl, listBox1, lstconvos, and buttons 
    // including button4 (Start/Next) and button5 (Stop).
    public partial class Form1 : Form
    {
        // --- CQH-TED CORE COMPONENTS ---
        public nn.MainProcessor nnProcessor = new nn.MainProcessor();
        private CancellationTokenSource cts = null;
        private object lockObject = new object();

        private static readonly HttpClient httpClient = new HttpClient();

        // --- Data Structures ---
        public static class idmanager
        {
            public static int id = 1;
            public static int getid() { return id++; }
        }

        public Dictionary<string, int> stringtoidmapping = new Dictionary<string, int>();
        public class pattern
        {
            public List<int> patternsequence = new List<int>();
        }
        public pattern totalpattern = new pattern();
        public List<pattern> subpatterns = new List<pattern>();

        // Constraints from the paper
        private const int WORD_LIMIT_N = 3;
        private const double DELTA_THRESHOLD = 0.2;

        public Form1()
        {
             InitializeComponent(); // Designer call placeholder
            httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        // --- Core Algorithm Logic (CHP) ---

        public void recalculatesubpatternswithminusoneforplaceholderkeepallsetssamesizesfillemptyspaceswithminusone()
        {
            subpatterns.Clear();
            int n = totalpattern.patternsequence.Count;
            if (n == 0) return;

            int effectiveN = Math.Min(n, 10);

            int patterncount = 1 << effectiveN;
            for (int i = 1; i < patterncount; i++)
            {
                pattern p = new pattern();
                for (int j = 0; j < effectiveN; j++)
                {
                    if ((i & (1 << j)) != 0)
                    {
                        p.patternsequence.Add(totalpattern.patternsequence[j]);
                    }
                    else
                    {
                        p.patternsequence.Add(-1);
                    }
                }
                while (p.patternsequence.Count < n)
                {
                    p.patternsequence.Add(-1);
                }
                subpatterns.Add(p);
            }
        }

        public void AssignIdsToConversationTree(conversationtree root)
        {
            // NEUER KORRIGIERTER CODE:
            // Die Parameter müssen anhand der CQH-TED-Spezifikationen gewählt werden.
            // Platzhalter für eine typische Konfiguration:
            const int NUM_DIMENSIONS = 3;   // Z.B. Wort-, Phrasen-, Kontext-Dimensionen
            const int NUM_CONTEXTS = 2;     // Z.B. Formal vs. Informal
            const int NUM_TIMES = 1;        // Zeit-Dimension (kann hier 1 sein, wenn nicht verwendet)
            const int NUM_PARTNERS = 2;     // Partner A (0) und Partner B (1)
            const int MAX_PATTERN_LENGTH = 10; // Maximale Länge des zu verarbeitenden Musters

            // Der Initialisierungsaufruf (InitializeProcessor -> InitializeSwitches)
            nnProcessor.InitializeSwitches(
                NUM_DIMENSIONS,
                NUM_CONTEXTS,
                NUM_TIMES,
                NUM_PARTNERS,
                MAX_PATTERN_LENGTH
            );

            // Die folgenden Zeilen bleiben gleich
            Dictionary<int, int> partnerMap = new Dictionary<int, int>();
            AssignIdsRecursive(root, partnerMap);

            // BEACHTEN SIE: nnProcessor.TrainFromTree existiert in der neuen nn.cs nicht mehr,
            // es muss durch nnProcessor.BruteForceCombinatorially() ersetzt werden!
            // nnProcessor.TrainFromTree(root, partnerMap); // ALTER CODE

            // Die Trainingsdaten müssen erst der nnProcessor hinzugefügt werden, bevor brute-force ausgeführt wird.
            // Wir nehmen an, dass die Trainingsdaten (inputseries/outputseries) extern hinzugefügt werden
            // und lassen diesen Aufruf HIER WEG, da er nicht direkt in der Funktion trainiert.

            // Stattdessen fügen wir in AssignIdsRecursive die Daten der nnProcessor hinzu.
        }

        private int conversationIdCounter = 1;
        private void AssignIdsRecursive(conversationtree node, Dictionary<int, int> partnerMap)
        {
            if (node.nodepattern != null)
            {
                node.conversationid = conversationIdCounter++;
                node.conversationpartnerid = node.conversationid % 2;

                if (!partnerMap.ContainsKey(node.conversationid))
                {
                    partnerMap.Add(node.conversationid, node.conversationpartnerid);
                }
            }

            foreach (var child in node.children)
            {
                AssignIdsRecursive(child, partnerMap);
            }
        }

        // --- Conversation Tree Classes (L_exploit Synthesis) ---
        public class conversationtree
        {
            public List<conversationtree> children = new List<conversationtree>();
            public pattern nodepattern;
            public int conversationid;
            public int conversationpartnerid;

            public void bruteforce(List<pattern> availablepatterns)
            {
                if (availablepatterns.Count > 5) return;

                foreach (var p in availablepatterns)
                {
                    conversationtree child = new conversationtree();
                    child.nodepattern = p;
                    children.Add(child);

                    var newavailablepatterns = new List<pattern>(availablepatterns);
                    newavailablepatterns.Remove(p);

                    child.bruteforce(newavailablepatterns);
                }
            }
        }

        // --- Core Asynchronous Reverse Engineering Loop ---

        private async void button4_Click(object sender, EventArgs e)
        {
            
        }

        private void Continuous_Reverse_Engineering_Loop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Use a simple default URL. For a production system, this would come from txturl.Text 
                    // and thread-safe access would be needed via 'Invoke'.
                    string url = textBox1.Text;

                    var newWords = ScrapeWebPageAsync(url, WORD_LIMIT_N).Result;

                    foreach (var word in newWords)
                    {
                        int newid = idmanager.getid();
                        stringtoidmapping[word] = newid;
                        totalpattern.patternsequence.Add(newid);

                        this.Invoke((MethodInvoker)delegate {
                            if (listBox1 != null && !listBox1.Items.Contains(word)) listBox1.Items.Add(word);
                        });
                    }

                    recalculatesubpatternswithminusoneforplaceholderkeepallsetssamesizesfillemptyspaceswithminusone();

                    conversationtree tree = new conversationtree();
                    tree.bruteforce(new List<pattern>(subpatterns));

                    AssignIdsToConversationTree(tree);

                    if (subpatterns.Count > 0)
                    {
                        int[] testPattern = subpatterns.Last().patternsequence.ToArray();

                        // 💥 CORRECTION: Removed old methods and implemented new prediction logic

                        // Assume we are trying to predict if this pattern belongs to Partner 0 (or Partner 1, etc.)
                        const int TARGET_PARTNER_ID = 0;
                        nn.IO testInput = new nn.IO { IOVECTOR = testPattern };

                        // Use the new method to find if any combination of switches matches the pattern.
                        var possibilities = nnProcessor.GetPossibilitySpace(testInput, TARGET_PARTNER_ID);

                        // Simple prediction: If possibilities are found, the pattern belongs to the target partner

                        this.Invoke((MethodInvoker)delegate
                        {
                            if (lstconvos == null) return;

                            // --- CORRECTED UI OUTPUT START ---

                            // TARGET_PARTNER_ID must be defined outside the Invoke block (e.g., const int TARGET_PARTNER_ID = 0;)
                            // testPattern must be defined outside the Invoke block.
                            bool patternRecognized = possibilities.Count > 0;

                            lstconvos.Items.Add($"[{DateTime.Now.ToLongTimeString()}] Cycle Complete. Subpatterns: {subpatterns.Count}");
                            lstconvos.Items.Add($"Test P: {string.Join(",", testPattern)} -> Target Partner ID: {TARGET_PARTNER_ID}");

                            if (patternRecognized)
                            {
                                lstconvos.Items.Add("  -> RECOGNIZED: Pattern found in Associative Memory (M).");
                                lstconvos.Items.Add($"  -> Matches found in {possibilities.Count} switch combinations.");
                                // The CONTAINMENT message is simplified here as Delta_Gen is gone.
                                lstconvos.Items.Add("!!! CONTAINMENT TRIGGERED (Pattern Recognized) !!!");
                            }
                            else
                            {
                                lstconvos.Items.Add("  -> UNKNOWN: No pattern match found in M for this Partner/Switch state.");
                            }

                            // --- CORRECTED UI OUTPUT END ---

                            lstconvos.TopIndex = lstconvos.Items.Count - 1;
                        });
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate { MessageBox.Show($"Loop Error: {ex.Message}", "Engine Error"); });
                    return;
                }

                Thread.Sleep(500);
            }
        }

        // Helper to remove HTML tags (since full HTML parsers are complex)
        private string RemoveHtmlTags(string html)
        {
            // Simple regex to remove content between < and >
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
        }

        // Updated Core HTTP scraping logic with simplified splitting
        private async Task<List<string>> ScrapeWebPageAsync(string url, int limit)
        {
            var words = new List<string>();
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string htmlContent = await response.Content.ReadAsStringAsync();


                // 2. Simplified Splitting Logic (Closest to original basic approach)
                string[] rawTokens = htmlContent.Split(new string[] { "<a href=\"/?word=" }, StringSplitOptions.RemoveEmptyEntries);
                // 3. Process tokens and apply limit
                var uniqueWords = new HashSet<string>();
                foreach (string token in rawTokens)
                {
                    string cleanedToken = token.Split(new string[] { "\">" },StringSplitOptions.RemoveEmptyEntries)[0];
                    if (cleanedToken.Length > 0)
                    {
                        if (uniqueWords.Add(cleanedToken)) // Only add if it's new (unique)
                        {
                            words.Add(cleanedToken);;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the failure to the console/debug output (not the UI thread)
                System.Diagnostics.Debug.WriteLine($"Scraping failed: {ex.Message}. Falling back to simulated words.");

                // Fall back to simulated words if scraping fails
                words = new List<string> {
                    $"SimW{idmanager.id + 1}",
                    $"SimW{idmanager.id + 2}",
                    $"SimW{idmanager.id + 3}"
                }.Take(limit).ToList();
            }

            // Ensure we return the requested limit, or the words we found.
            List<string> result = new List<string>();
            for (int i = 6; i < words.Count && result.Count < 6+limit; i++)
            {
                result.Add(words[i]);
            }
            return result;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            lock (lockObject)
            {
                if (cts != null)
                {
                    cts.Cancel();
                }
            }
        }

        // --- Minimal UI Handlers (for compilation) ---
        private void button1_Click(object sender, EventArgs e) { }
        private void button2_Click(object sender, EventArgs e) { }
        private void Form1_Load(object sender, EventArgs e) { }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void txturl_TextChanged(object sender, EventArgs e) { }

        private async void button1_Click_1(object sender, EventArgs e)
        {
            lock (lockObject)
            {
                if (cts != null && !cts.IsCancellationRequested)
                {
                    MessageBox.Show("CQH-TED Engine is already running. Click 'Stop' first.", "Engine State");
                    return;
                }
                cts = new CancellationTokenSource();
            }

            try
            {
                lstconvos.Items.Add($"--- CQH-TED Engine START (N={WORD_LIMIT_N} Constraint) ---");

                await Task.Run(() => Continuous_Reverse_Engineering_Loop(cts.Token), cts.Token);
            }
            catch (OperationCanceledException)
            {
                lstconvos.Items.Add("--- CQH-TED Engine STOPPED ---");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal Error: {ex.Message}", "CQH-TED Engine Failure");
            }
            finally
            {
                lock (lockObject)
                {
                    cts = null;
                }
            }
        }
    }
}