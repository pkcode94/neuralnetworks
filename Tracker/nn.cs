using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Tracker.nn;

namespace Tracker
{//unlearnable
    internal class nn
    {
        //self referential compound feedback loop
        //blind feedback loop
        //transition
        //input invariance threshold
        //bruteforce conversation fragments to neural network mapping until pattern is consistent
        //gradient descent relative to input
        //bruteforcing combinatoric iomapping gradient X input where the gradient strengthens cumulatively with each successive input 
        //gradient decay
        //normalization
        //learning rate
        //extract sensitive variables
        //io ambiguity
        //decreasing options from infinite possible patterns will eventually hit the right pattern
        //bruteforcing common noise using gravity and location i know that you know that i know....
        //consider sensory neuron count when crawling google
        //abe to decrypt encryption by learning patterns
        //every encryption system is a deterministic information transformation system posed as a stochastic system
        public class IO
        {
            public int[] IOVECTOR;
        }
        public class compositedimensionvector
        {
            // bool active = true;
            public int length;
            public int offset;
        }
        public class IOdimensionsswitch
        {
            public List<IOdimensions> dimensions = new List<IOdimensions>();
            public int activedimensionindex = 0;
            public IOdimensions activedimensions
            {
                get
                {
                    return dimensions[activedimensionindex];
                }
            }
        }
        public class ipvbruteforce
        {

        }
        public class pingscanner
        {

        }
        public class IOdimensions
        {
            public List<compositedimensionvector> dimensionvector = new List<compositedimensionvector>();
            public int maxlength;
            public int offsetininputseries = 0;
            public void generatecompositedimensionvector()
            {
                for (int i = 0; i < maxlength; i++)
                {
                     
                }
            }

            public class iomapping
            {
                public Dictionary<int[], int[]> iomatrix = new Dictionary<int[], int[]>();
            }
            public void populateinputvectors(List<IO> inputseries)
            {
                foreach (var dimension in dimensionvector)
                {
                    foreach (var input in inputseries)
                    {
                        if (input.IOVECTOR.Count() >= dimension.offset + dimension.length && dimension.offset >= 0 && dimension.length > 0 && dimension.dimensionindex < maxlength)
                        {
                            int[] slice = new int[dimension.length];
                            Array.Copy(input.IOVECTOR, dimension.offset, slice,offsetininputseries, dimension.length);
                            iomapping mapping = new iomapping();
                            if (!mapping.iomatrix.ContainsKey(slice))
                            {
                                int[] output = mapping.iomatrix[slice];
                            }
                        }
                    }
                }
            }


        }
        IOdimensionsswitch inputswitch = new IOdimensionsswitch();
        IOdimensionsswitch outputswitch = new IOdimensionsswitch();
        public List<IO> inputseries = new List<IO>();
        public List<IO> outputseries = new List<IO>();
        public void processinputseries()
        {
            foreach (var input in inputseries)
            {
                foreach (var inputdimension in inputswitch.dimensions)
                {
                    inputdimension.populateinputvectors(inputseries);
                }
            }
        }
    }
}
