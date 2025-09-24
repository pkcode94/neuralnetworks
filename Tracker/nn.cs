using System;
using System.Collections.Generic;
using System.Linq;

namespace Tracker
{
    // Main class acting as a container for all related classes.
    internal class nn
    {
        // Data structure to hold an input/output vector.
        public class IO
        {
            public int[] IOVECTOR;
        }

        // Data structure to define a slice of the input/output vector.
        public class compositedimensionvector
        {
            public int length;
            public int offset;
        }

        // Manages a list of different IOdimensions objects.
        public class DimensionalSwitch
        {
            public List<IOdimensions> dimensions = new List<IOdimensions>();
            public int activeIndex = 0;

            public IOdimensions ActiveDimension
            {
                get
                {
                    if (dimensions.Count == 0 || activeIndex >= dimensions.Count)
                    {
                        return null;
                    }
                    return dimensions[activeIndex];
                }
            }
        }

        // Manages the contextual state.
        public class ContextualSwitch
        {
            public int activeIndex = 0;
            public int maxIndex = 0;
        }

        // Manages the time-based state.
        public class TimeSwitch
        {
            public int activeIndex = 0;
            public int maxIndex = 0;
        }

        // Defines and populates the specific dimensions and their mappings.
        public class IOdimensions
        {
            public List<compositedimensionvector> dimensionvector = new List<compositedimensionvector>();
            public int maxlength;

            // Dictionary to store the mapping from an input slice and a switch index to an output slice.
            public Dictionary<Tuple<int[], int, int>, int[]> iomatrix = new Dictionary<Tuple<int[], int, int>, int[]>(new TupleIntArrayIntIntEqualityComparer());

            public void generatecompositedimensionvector()
            {
                for (int offset = 0; offset < maxlength; offset++)
                {
                    for (int length = 1; offset + length <= maxlength; length++)
                    {
                        compositedimensionvector dimension = new compositedimensionvector();
                        dimension.offset = offset;
                        dimension.length = length;
                        dimensionvector.Add(dimension);
                    }
                }
            }

            // Populates the iomatrix with input-output pairs based on the given contextual and time switches.
            public void populateiomatrix(List<IO> inputseries, List<IO> outputseries, ContextualSwitch contextSwitch, TimeSwitch timeSwitch)
            {
                for (int i = 0; i < inputseries.Count && i < outputseries.Count; i++)
                {
                    var input = inputseries[i];
                    var output = outputseries[i];

                    foreach (var dimension in dimensionvector)
                    {
                        if (input.IOVECTOR.Length >= dimension.offset + dimension.length &&
                            output.IOVECTOR.Length >= dimension.offset + dimension.length)
                        {
                            int[] inputSlice = new int[dimension.length];
                            int[] outputSlice = new int[dimension.length];
                            Array.Copy(input.IOVECTOR, dimension.offset, inputSlice, 0, dimension.length);
                            Array.Copy(output.IOVECTOR, dimension.offset, outputSlice, 0, dimension.length);

                            var key = new Tuple<int[], int, int>(inputSlice, contextSwitch.activeIndex, timeSwitch.activeIndex);
                            if (!iomatrix.ContainsKey(key))
                            {
                                iomatrix.Add(key, outputSlice);
                            }
                        }
                    }
                }
            }
        }

        // Custom equality comparer for integer arrays.
        public class IntArrayEqualityComparer : IEqualityComparer<int[]>
        {
            public bool Equals(int[] x, int[] y)
            {
                if (x == null || y == null) return false;
                if (x.Length != y.Length) return false;
                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i]) return false;
                }
                return true;
            }

            public int GetHashCode(int[] obj)
            {
                if (obj == null) return 0;
                unchecked
                {
                    int hash = 17;
                    foreach (var element in obj)
                    {
                        hash = hash * 31 + element.GetHashCode();
                    }
                    return hash;
                }
            }
        }

        // Custom equality comparer for the Tuple key.
        public class TupleIntArrayIntIntEqualityComparer : IEqualityComparer<Tuple<int[], int, int>>
        {
            private readonly IEqualityComparer<int[]> intArrayComparer = new IntArrayEqualityComparer();

            public bool Equals(Tuple<int[], int, int> x, Tuple<int[], int, int> y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
                return intArrayComparer.Equals(x.Item1, y.Item1) && x.Item2 == y.Item2 && x.Item3 == y.Item3;
            }

            public int GetHashCode(Tuple<int[], int, int> obj)
            {
                if (obj == null) return 0;
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + intArrayComparer.GetHashCode(obj.Item1);
                    hash = hash * 31 + obj.Item2.GetHashCode();
                    hash = hash * 31 + obj.Item3.GetHashCode();
                    return hash;
                }
            }
        }

        // The primary class that holds the series data and manages the switches.
        public class MainProcessor
        {
            private DimensionalSwitch inputDimensionalSwitch = new DimensionalSwitch();
            private DimensionalSwitch outputDimensionalSwitch = new DimensionalSwitch();
            private ContextualSwitch inputContextualSwitch = new ContextualSwitch();
            private ContextualSwitch outputContextualSwitch = new ContextualSwitch();
            private TimeSwitch inputTimeSwitch = new TimeSwitch();
            private TimeSwitch outputTimeSwitch = new TimeSwitch();

            public List<IO> inputseries = new List<IO>();
            public List<IO> outputseries = new List<IO>();

            public void InitializeSwitches(int numDimensions, int numContexts, int numTimes, int maxLength)
            {
                inputContextualSwitch.maxIndex = numContexts - 1;
                outputContextualSwitch.maxIndex = numContexts - 1;
                inputTimeSwitch.maxIndex = numTimes - 1;
                outputTimeSwitch.maxIndex = numTimes - 1;

                for (int i = 0; i < numDimensions; i++)
                {
                    var inputDim = new IOdimensions();
                    inputDim.maxlength = maxLength;
                    inputDim.generatecompositedimensionvector();
                    inputDimensionalSwitch.dimensions.Add(inputDim);

                    var outputDim = new IOdimensions();
                    outputDim.maxlength = maxLength;
                    outputDim.generatecompositedimensionvector();
                    outputDimensionalSwitch.dimensions.Add(outputDim);
                }
            }

            // Sets the active context to the default (index 0).
            public void SetDefaultContext()
            {
                inputContextualSwitch.activeIndex = 0;
                outputContextualSwitch.activeIndex = 0;
            }

            public void BruteForceCombinatorially()
            {
                Console.WriteLine("Brute-forcing combinatorial switches...");
                for (int dimIndex = 0; dimIndex < inputDimensionalSwitch.dimensions.Count; dimIndex++)
                {
                    for (int contextIndex = 0; contextIndex <= inputContextualSwitch.maxIndex; contextIndex++)
                    {
                        SwitchDimensionalContext(dimIndex);
                        SwitchContextualMode(contextIndex);

                        var activeInputDimension = inputDimensionalSwitch.ActiveDimension;
                        var activeOutputDimension = outputDimensionalSwitch.ActiveDimension;
                        if (activeInputDimension != null && activeOutputDimension != null)
                        {
                            activeInputDimension.populateiomatrix(inputseries, outputseries, inputContextualSwitch, inputTimeSwitch);
                        }
                    }
                }
                Console.WriteLine("Brute-force complete.");
            }

            public List<Tuple<int, int, int>> GetPossibilitySpace(IO new_input)
            {
                List<Tuple<int, int, int>> possibilities = new List<Tuple<int, int, int>>();

                for (int dimIndex = 0; dimIndex < inputDimensionalSwitch.dimensions.Count; dimIndex++)
                {
                    for (int contextIndex = 0; contextIndex <= inputContextualSwitch.maxIndex; contextIndex++)
                    {
                        for (int timeIndex = 0; timeIndex <= inputTimeSwitch.maxIndex; timeIndex++)
                        {
                            SwitchDimensionalContext(dimIndex);
                            SwitchContextualMode(contextIndex);
                            SwitchTime(timeIndex);

                            var activeInputDimension = inputDimensionalSwitch.ActiveDimension;
                            if (activeInputDimension != null)
                            {
                                foreach (var dimensionVector in activeInputDimension.dimensionvector)
                                {
                                    if (new_input.IOVECTOR.Length >= dimensionVector.offset + dimensionVector.length)
                                    {
                                        int[] inputSlice = new int[dimensionVector.length];
                                        Array.Copy(new_input.IOVECTOR, dimensionVector.offset, inputSlice, 0, dimensionVector.length);

                                        var key = new Tuple<int[], int, int>(inputSlice, contextIndex, timeIndex);

                                        if (activeInputDimension.iomatrix.ContainsKey(key))
                                        {
                                            possibilities.Add(new Tuple<int, int, int>(dimIndex, contextIndex, timeIndex));
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return possibilities.Distinct().ToList();
            }

            public void SwitchDimensionalContext(int index)
            {
                if (index >= 0 && index < inputDimensionalSwitch.dimensions.Count)
                {
                    inputDimensionalSwitch.activeIndex = index;
                    outputDimensionalSwitch.activeIndex = index;
                }
            }

            public void SwitchContextualMode(int index)
            {
                if (index >= 0 && index <= inputContextualSwitch.maxIndex)
                {
                    inputContextualSwitch.activeIndex = index;
                    outputContextualSwitch.activeIndex = index;
                }
            }

            public void SwitchTime(int index)
            {
                if (index >= 0 && index <= inputTimeSwitch.maxIndex)
                {
                    inputTimeSwitch.activeIndex = index;
                    outputTimeSwitch.activeIndex = index;
                }
            }
        }
    }
}