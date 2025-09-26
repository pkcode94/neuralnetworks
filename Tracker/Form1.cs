using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;


using System;
using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using static TorchSharp.torch.nn;
using System.Security.Cryptography;


namespace Tracker
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public static class idmanager
        {

            public static int id;
            public static int getid()
            {
                return id++;
            }
        }



        public Dictionary<string, int> stringtoidmapping = new Dictionary<string, int>();
        public class pattern
        {
            public List<int> patternsequence = new List<int>();
        }
        public pattern totalpattern = new pattern();
        public List<pattern> subpatterns = new List<pattern>();
        public List<List<pattern>> PossibleConversations = new List<List<pattern>>();
        
        public class conversationunit
        {
            public pattern Question;
            public pattern Answer;
        }
        public class encryptedconversation
        {
            public List<conversationunit> possibleseriesone = new List<conversationunit>();
            public List<conversationunit> possibleseriestwo = new List<conversationunit>();
        }
        public class conversation
        {
            public List<conversationunit> ActualSeries = new List<conversationunit>();
        }
        private static readonly HttpClient client = new HttpClient();
        //calculate the powerset of all current patterns
        public void recalculatesubpatternswithminusoneforplaceholderkeepallsetssamesizesfillemptyspaceswithminusone()
        {
            subpatterns.Clear();
            int n = totalpattern.patternsequence.Count;
            int patterncount = 1 << n; // 2^n possible patterns
            for (int i = 1; i < patterncount; i++) // start from 1 to avoid empty pattern
            {
                pattern p = new pattern();
                for (int j = 0; j < n; j++)
                {
                    if ((i & (1 << j)) != 0)
                    {
                        p.patternsequence.Add(totalpattern.patternsequence[j]);
                    }
                    else
                    {
                        p.patternsequence.Add(-1); // placeholder for missing element
                    }
                }
                subpatterns.Add(p);
            }
        }
        public Dictionary<int, int> PossibleConversationidToConversationPartnerMapping = new Dictionary<int, int>();
       
        public void CombinatoricallyBruteforcePossibleConversationidToConversationPartnerMapping()
        {
            
        }
        public pattern subtractpatternsetfrompatternset(pattern original, pattern toremove)
        {
            pattern result = new pattern();
            foreach (var id in original.patternsequence)
            {
                if (!toremove.patternsequence.Contains(id))
                {
                    result.patternsequence.Add(id);
                }
            }
            return result;
        }
       
        public void convertpowersetofpatternstoconversationwhereelementsareuniquefromtotalpatterns()
        {
            
        }
        public class conversationtree
        {
            public List<conversationtree> children = new List<conversationtree>();
            public pattern nodepattern;
            public conversationtree parent;
            public int conversationid;
            public int conversationpartnerid;
            public void removepatternswithambiguitiesatpositionexceptwherevalueisminusone(List<pattern> availablepatterns)
            {

                List<pattern> copy = new List<pattern>(availablepatterns);
                foreach (var pattern in availablepatterns)
                {
                    for (int i = 0; i < pattern.patternsequence.Count; i++)
                    {
                        if (pattern.patternsequence[i] != -1)
                        {
                            foreach (var otherpattern in availablepatterns)
                            {
                                if (otherpattern != pattern && otherpattern.patternsequence[i] == pattern.patternsequence[i])
                                {
                                   copy.Remove(otherpattern);
                                    break;
                                }
                            }
                        }
                    }
                }



                }
            public void bruteforce(List<pattern> availablepatterns)
            {
                removepatternswithambiguitiesatpositionexceptwherevalueisminusone(availablepatterns);
                if (availablepatterns.Count == 0)
                {
                    return;
                }
                foreach (var p in availablepatterns)
                {
                    conversationtree child = new conversationtree();
                    child.nodepattern = p;
                    child.parent = this;
                    children.Add(child);
                    var newavailablepatterns = new List<pattern>(availablepatterns);
                    newavailablepatterns.Remove(p);
                    child.bruteforce(newavailablepatterns);
                }
            }
            
            public void compiletosignal()
            {

            }
            public void reversefromsignal()
            {

            }
        }
        
   

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string url = txturl.Text;

            // This line blocks the thread until the response is received
            HttpResponseMessage response = client.GetAsync(url).Result;

            // Throws an exception on non-success status codes
            response.EnsureSuccessStatusCode();

            // This line blocks the thread until the content is read
            string content = response.Content.ReadAsStringAsync().Result;
            string[] split = content.Split(new string[] { "<a href=\"/?word=" }, StringSplitOptions.RemoveEmptyEntries);
            List<string> result = new List<string>();
            for (int i = 10; i < split.Count(); i++)
            {
                result.Add(split[i].Split(new string[] { "\">" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                listBox1.Items.Add(result[result.Count - 1]);
                listBox1.TopIndex = listBox1.Items.Count - 1;
                int newid = idmanager.getid();
                stringtoidmapping[result[result.Count - 1]] = newid;
                totalpattern.patternsequence.Add(newid);

            }
            recalculatesubpatternswithminusoneforplaceholderkeepallsetssamesizesfillemptyspaceswithminusone();
            conversationtree tree = new conversationtree();
            tree.bruteforce(subpatterns);
            lstconvos.Items.Clear();
            foreach (var convo in PossibleConversations)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var p in convo)
                {
                    foreach (var id in p.patternsequence)
                    {
                        var str = stringtoidmapping.FirstOrDefault(x => x.Value == id).Key;
                        sb.Append(str + " ");
                    }
                    sb.Append("| ");
                }
                lstconvos.Items.Add(sb.ToString());
                lstconvos.TopIndex = lstconvos.Items.Count - 1;
            }
        }

        private void txturl_TextChanged(object sender, EventArgs e)
        {

        }
    }
}