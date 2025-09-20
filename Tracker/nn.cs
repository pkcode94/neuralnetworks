using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tracker
{
    internal class nn
    {
        //self referential feedback loop
        //blind feedback loop
        //transition
        //input invariance threshold
        //bruteforce conversation fragments to neural network mapping until pattern is consistent
        //gradient descent relative to input
        //bruteforcing combinatoric iomapping gradient X input where the gradient strengthens cumulatively with each successive input 
        //gradient decay
        //normalization
        //learning rate
        public class IO
        {
            public int[] IOVECTOR;
        }
        public class inputdimensions
        {
            public int maxlength;
            public int offset = 0;
        }
        public class iomapping
        {
            public Dictionary<int[], int[]> iomatrix = new Dictionary<int[], int[]>();
        }
    }
}
