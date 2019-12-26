using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ForestFinal.Forest
{
    public class Region
    {
        private static readonly object myLock = new object();
        private static Region region = null;
        public int RegionCount { get; set; }
        public ConcurrentDictionary<int, ConcurrentDictionary<string, RegionalNode>> Regions;

        public Region()
        {
            RegionCount = 0;
            Regions = new ConcurrentDictionary<int, ConcurrentDictionary<string, RegionalNode>>();
        }

        public static Region GetInstance()
        {
            if (region == null)
            {
                lock (myLock)
                {
                    if (region == null)
                    {
                        region = new Region();
                    }
                }
            }
            return region;
        }

        public void Update(IEnumerable<Node> nodes)  // TODO: add probability measures here
        {
            ConcurrentDictionary<string, RegionalNode> newRegion = new ConcurrentDictionary<string, RegionalNode>();
            foreach (Node node in nodes)
            {
                newRegion.TryAdd(node.NodeID, new RegionalNode(node, null));
            }
            Regions.TryAdd(RegionCount, newRegion);
            RegionCount += 1;
        }

        public double GetHistoricalProbability(int step, string nodeID)
        {
            double sumProbability = 0;

            Regions.TryGetValue(step, out ConcurrentDictionary<string, RegionalNode> region);
            if (region == null)
            {
                return 0;
            }
            region.TryGetValue(nodeID, out RegionalNode node);
            if (node == null)
            {
                return 0;
            }
            foreach (KeyValuePair<string, RegionalNode> kv in region)
            {
                sumProbability += kv.Value.Probability;
            }
            if (sumProbability == node.Probability)
            {
                return 1;
            }
            double auxiliarySumProbability = (region.Count - 1) * sumProbability;
            double probability = (sumProbability - node.Probability) * (1 / auxiliarySumProbability);
            return probability;
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
    }
}
