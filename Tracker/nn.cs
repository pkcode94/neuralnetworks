using System;
using System.Collections.Generic;
using System.Linq;

namespace Tracker
{
    // Main class acting as a container for all related classes (The Associative Memory).
    public class nn
    {
        // Data structure to hold an input/output vector (The Conversation/Pattern Sequence).
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

        // Manages a list of different IOdimensions objects (The Dimensional Switch).
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

        // Manages the active conversation partner ID (The Partner Switch).
        public class PartnerSwitch
        {
            public int activeIndex = 0;
            public int maxIndex = 0;
        }

        // Defines and populates the specific dimensions and their mappings (The Associative Memory (M)).
        public class IOdimensions
        {
            // All possible slices of the max input length.
            public List<compositedimensionvector> dimensionvector = new List<compositedimensionvector>();
            public int maxlength;

            /* * The Multi-Dimensional Associative Memory Matrix (iomatrix):
             * Key: Tuple<Input Slice, Context Index, Time Index, Partner ID>
             * Value: Output Slice
             */
            public Dictionary<Tuple<int[], int, int, int>, int[]> iomatrix =
                new Dictionary<Tuple<int[], int, int, int>, int[]>(new TupleIntArrayIntIntIntEqualityComparer());

            // Generates all possible slice offsets and lengths up to maxlength.
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

            // Populates the iomatrix with input-output pairs across all sub-dimensions.
            public void populateiomatrix(List<IO> inputseries, List<IO> outputseries, ContextualSwitch contextSwitch, TimeSwitch timeSwitch, PartnerSwitch partnerSwitch)
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

                            // The critical multi-dimensional key including the Partner ID.
                            var key = new Tuple<int[], int, int, int>(
                                inputSlice,
                                contextSwitch.activeIndex,
                                timeSwitch.activeIndex,
                                partnerSwitch.activeIndex); // Partner ID

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
        public class TupleIntArrayIntIntIntEqualityComparer : IEqualityComparer<Tuple<int[], int, int, int>>
        {
            private readonly IEqualityComparer<int[]> intArrayComparer = new IntArrayEqualityComparer();

            public bool Equals(Tuple<int[], int, int, int> x, Tuple<int[], int, int, int> y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
                return intArrayComparer.Equals(x.Item1, y.Item1)
                    && x.Item2 == y.Item2
                    && x.Item3 == y.Item3
                    && x.Item4 == y.Item4; // Compare Partner ID
            }

            public int GetHashCode(Tuple<int[], int, int, int> obj)
            {
                if (obj == null) return 0;
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + intArrayComparer.GetHashCode(obj.Item1);
                    hash = hash * 31 + obj.Item2.GetHashCode();
                    hash = hash * 31 + obj.Item3.GetHashCode();
                    hash = hash * 31 + obj.Item4.GetHashCode(); // Include partner index
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
            private PartnerSwitch partnerSwitch = new PartnerSwitch();

            public List<IO> inputseries = new List<IO>();
            public List<IO> outputseries = new List<IO>();

            // Initializes all switches and dimensions based on system parameters.
            public void InitializeSwitches(int numDimensions, int numContexts, int numTimes, int numPartners, int maxLength)
            {
                inputContextualSwitch.maxIndex = numContexts - 1;
                outputContextualSwitch.maxIndex = numContexts - 1;
                inputTimeSwitch.maxIndex = numTimes - 1;
                outputTimeSwitch.maxIndex = numTimes - 1;
                partnerSwitch.maxIndex = numPartners - 1; // Set the max partner index

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

            public void SetDefaultContext()
            {
                SwitchContextualMode(0);
                SwitchTime(0);
                SwitchPartner(0);
            }

            public void SwitchPartner(int index)
            {
                if (index >= 0 && index <= partnerSwitch.maxIndex)
                {
                    partnerSwitch.activeIndex = index;
                }
            }

            // The C-A PTA (Combinatorial Associative Pattern Training Algorithm)
            public void BruteForceCombinatorially()
            {
                Console.WriteLine("Brute-forcing combinatorial switches and partners...");

                // Outer loop iterates through all Partner IDs for separate training spaces.
                for (int partnerIndex = 0; partnerIndex <= partnerSwitch.maxIndex; partnerIndex++)
                {
                    SwitchPartner(partnerIndex);
                    Console.WriteLine($"  - Training for Partner ID: {partnerIndex}");

                    // Inner loops iterate through Dimensional and Contextual switches.
                    for (int dimIndex = 0; dimIndex < inputDimensionalSwitch.dimensions.Count; dimIndex++)
                    {
                        for (int contextIndex = 0; contextIndex <= inputContextualSwitch.maxIndex; contextIndex++)
                        {
                            // Time Switch is often handled incrementally, but here we brute-force it too for completeness.
                            for (int timeIndex = 0; timeIndex <= inputTimeSwitch.maxIndex; timeIndex++)
                            {
                                SwitchDimensionalContext(dimIndex);
                                SwitchContextualMode(contextIndex);
                                SwitchTime(timeIndex);

                                var activeInputDimension = inputDimensionalSwitch.ActiveDimension;
                                var activeOutputDimension = outputDimensionalSwitch.ActiveDimension;
                                if (activeInputDimension != null && activeOutputDimension != null)
                                {
                                    // Populate the memory matrix for the current combination of (Dim, Context, Time, Partner)
                                    activeInputDimension.populateiomatrix(inputseries, outputseries, inputContextualSwitch, inputTimeSwitch, partnerSwitch);
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("Brute-force C-A PTA complete.");
            }

            // Queries the Associative Memory (M) for a specific new input and target partner.
            public List<Tuple<int, int, int, int>> GetPossibilitySpace(IO new_input, int targetPartnerIndex)
            {
                List<Tuple<int, int, int, int>> possibilities = new List<Tuple<int, int, int, int>>();

                // Set the partner switch to the target index for a focused lookup.
                SwitchPartner(targetPartnerIndex);

                // Iterate through all possible states to find a matching pattern slice.
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
                                // Check all possible slice vectors (sub-dimensions) in the active dimension.
                                foreach (var dimensionVector in activeInputDimension.dimensionvector)
                                {
                                    if (new_input.IOVECTOR.Length >= dimensionVector.offset + dimensionVector.length)
                                    {
                                        int[] inputSlice = new int[dimensionVector.length];
                                        Array.Copy(new_input.IOVECTOR, dimensionVector.offset, inputSlice, 0, dimensionVector.length);

                                        // The key to check against the memory matrix.
                                        var key = new Tuple<int[], int, int, int>(inputSlice, contextIndex, timeIndex, targetPartnerIndex);

                                        if (activeInputDimension.iomatrix.ContainsKey(key))
                                        {
                                            // Found a match: log the combination of switches that led to it.
                                            possibilities.Add(new Tuple<int, int, int, int>(dimIndex, contextIndex, timeIndex, targetPartnerIndex));

                                            // Once a match is found in a dimension, move to the next combination.
                                            goto NextCombination;
                                        }
                                    }
                                }
                            }
                        NextCombination:; // Label for the goto.
                        }
                    }
                }
                return possibilities.Distinct().ToList();
            }

            // State management methods for the switches.
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