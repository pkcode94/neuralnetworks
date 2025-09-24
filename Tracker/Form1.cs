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
        public void recalculatesubpatterns()
        {
            subpatterns.Clear();
            int n = totalpattern.patternsequence.Count;
            for (int i = 0; i < (1 << n); i++)
            {
                pattern p = new pattern();
                for (int j = 0; j < n; j++)
                {
                    if ((i & (1 << j)) > 0)
                    {
                        p.patternsequence.Add(totalpattern.patternsequence[j]);
                    }
                }
                if (p.patternsequence.Count > 0)
                    subpatterns.Add(p);
            }
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
            for (int i = 0; i < subpatterns.Count; i++)
            {
                for (int j = 0; j < subpatterns.Count; j++)
                {
                    if (i != j)
                    {
                        //if patterns are disjoint
                        if (!subpatterns[i].patternsequence.Intersect(subpatterns[j].patternsequence).Any())
                        {
                            pattern combined = new pattern();
                            combined.patternsequence.AddRange(subpatterns[i].patternsequence);
                            combined.patternsequence.AddRange(subpatterns[j].patternsequence);
                            if (combined.patternsequence.Count == totalpattern.patternsequence.Count)
                            {
                                PossibleConversations.Add(new List<pattern> { subpatterns[i], subpatterns[j] });
                            }
                        }
                    }
                }
            }
        }
        
        private void timer1_Tick(object sender, EventArgs e)
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
                recalculatesubpatterns();
                convertpowersetofpatternstoconversationwhereelementsareuniquefromtotalpatterns();
                lstconvos.Items.Clear();
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
            recalculatesubpatterns();convertpowersetofpatternstoconversationwhereelementsareuniquefromtotalpatterns();
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