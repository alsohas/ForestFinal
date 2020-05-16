using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ForestFinal.Forest
{
    [Serializable()]
    public class Region
    {
        //private static readonly object myLock = new object();
        //private static Region region = null;
        public int RegionCount = 0;
        public ConcurrentDictionary<int, ConcurrentDictionary<string, RegionalNode>> Regions = new ConcurrentDictionary<int, ConcurrentDictionary<string, RegionalNode>>();
        //public ConcurrentDictionary<int, ConcurrentDictionary<string, RegionalNode>> Costs = new ConcurrentDictionary<int, ConcurrentDictionary<string, RegionalNode>>();
        public ConcurrentDictionary<int, double> Costs = new ConcurrentDictionary<int, double>();
        public HashSet<string> ObsoleteNodes = new HashSet<string>();


        private static ThreadLocal<Region> instances = new ThreadLocal<Region>(() => new Region());

        private Region() { }
        public static Region Instance
        {
            get { return instances.Value; }
        }

        /// <summary>
        /// This method only add regional nodes to the first region.
        /// Subsequent node additions to latter regions are done inside of <see cref="PredictiveForest.Update(System.Device.Location.GeoCoordinate, double)"/>.
        /// </summary>
        /// <param name="nodes"></param>
        public void Update(IEnumerable<Node> nodes)
        {
            ConcurrentDictionary<string, RegionalNode> newRegion = new ConcurrentDictionary<string, RegionalNode>();
            foreach (Node node in nodes)
            {
                RegionalNode regionalNode = new RegionalNode(node, null);
                regionalNode.Probability = 1;
                newRegion.TryAdd(node.NodeID, regionalNode);
            }
            Regions.TryAdd(RegionCount, newRegion);
            RegionCount += 1;
        }

        public Dictionary<string, double> GetHistoricalProbabilities(int step)
        {
            Dictionary<string, double> probabilities = new Dictionary<string, double>();
            double sumProbability = 0;

            Regions.TryGetValue(step, out ConcurrentDictionary<string, RegionalNode> region);
            if (region == null)
            {
                return null;
            }

            if (region.Count == 1)
            {
                foreach (KeyValuePair<string, RegionalNode> kv in region)
                {
                    probabilities[kv.Key] = 1.0;
                    return probabilities;
                }
            }

            foreach (KeyValuePair<string, RegionalNode> kv in region)
            {
                sumProbability += kv.Value.Probability;
            }
            double auxiliarySumProbability = (region.Count - 1) * sumProbability;

            foreach (KeyValuePair<string, RegionalNode> kv in region)
            {
                RegionalNode node = kv.Value;
                double probability = (sumProbability - node.Probability) * (1 / auxiliarySumProbability);

                probabilities[kv.Key] = probability;
            }
            return probabilities;
        }

        public Dictionary<string, double> GetHistoricalProbabilitiesWithCost(int step)
        {
            if (!Costs.TryGetValue(step, out var cost))
            {
                GetHistoricalProbabilities(step);
            }

            Dictionary<string, double> probabilities = new Dictionary<string, double>();
            double sumProbability = 0;

            Regions.TryGetValue(step, out ConcurrentDictionary<string, RegionalNode> region);

            foreach (KeyValuePair<string, RegionalNode> kv in region)
            {
                sumProbability += Math.Abs(cost - kv.Value.Probability);
            }
            double auxiliarySumProbability = (region.Count - 1) * sumProbability;

            foreach (KeyValuePair<string, RegionalNode> kv in region)
            {
                double probability = (sumProbability - Math.Abs(cost - kv.Value.Probability)) * (1 / auxiliarySumProbability);
                probabilities[kv.Key] = probability;
            }
            return probabilities;
        }

        public void Reset()
        {
            RegionCount = 0;
            Regions.Clear();
        }

        public override string ToString()
        {
            return GetHashCode().ToString();
        }

        internal void AddCost(int currentStep, double cost)
        {
            Costs[currentStep] = cost;
        }
    }
}
