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
        private static readonly HttpClient client = new HttpClient();
        public void recalculatesubpatterns()
        {
             
        }
        Dictionary<Tuple<double, double>, int[]> deltacoordinatestopatternidmapping;
        private void timer1_Tick(object sender, EventArgs e)
        {
            string url = "https://gematrix.org";

            // This line blocks the thread until the response is received
            HttpResponseMessage response = client.GetAsync(url).Result;

            // Throws an exception on non-success status codes
            response.EnsureSuccessStatusCode();

            // This line blocks the thread until the content is read
            string content = response.Content.ReadAsStringAsync().Result;
            string[] split = content.Split(new string[]{ "<a href=\"/?word=" },StringSplitOptions.RemoveEmptyEntries);
            List<string> result = new List<string>();
            for(int i = 10; i < split.Count(); i++)
            {
                result.Add(split[i].Split(new string[] { "\">" }, StringSplitOptions.RemoveEmptyEntries)[0]);
                listBox1.Items.Add(result[result.Count-1]);
                listBox1.TopIndex = listBox1.Items.Count - 1;
                int newid = idmanager.getid();
                stringtoidmapping[result[result.Count - 1]] = newid;
                totalpattern.patternsequence.Add(newid);
                recalculatesubpatterns();
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
    }
}
