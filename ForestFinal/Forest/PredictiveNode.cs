using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForestFinal.Forest
{
    [Serializable()]
    public class PredictiveNode
    {
        public PredictiveNode(RoadNetwork roadNetwork, Node root, double cost, int depth, int maxDepth, Node parent,
                              ConcurrentDictionary<int, ConcurrentDictionary<string, List<PredictiveNode>>> predictiveRegions)
        {
            if (parent != null)
            {
                Parent = parent;
            }
            RoadNetwork = roadNetwork;
            Root = root;
            Cost = cost;
            Depth = depth;
            MaxDepth = maxDepth;
            Level = MaxDepth - Depth;

            PredictiveRegions = predictiveRegions;
            PredictiveRegions = predictiveRegions;
            WeightedChildren = new SortedDictionary<double, PredictiveNode>();
            Children = new Dictionary<string, PredictiveNode>();

            AddRegionReference();
        }

        private void AddRegionReference()
        {
            PredictiveRegions.TryGetValue(Level, out ConcurrentDictionary<string, List<PredictiveNode>> region);
            if (region == null)
            {
                region = new ConcurrentDictionary<string, List<PredictiveNode>>();
                PredictiveRegions.TryAdd(Level, region);
            }

            region.TryGetValue(Root.NodeID, out List<PredictiveNode> nodeList);
            if (nodeList == null)
            {
                nodeList = new List<PredictiveNode>();
                region.TryAdd(Root.NodeID, nodeList);
            }
            lock (nodeList)
            {
                nodeList.Add(this);
            }
        }

        public void Expand()
        {
            if (Depth == 0 || Level == MaxDepth)
            {
                return;
            }

            List<Task> taskList = new List<Task>();

            foreach (KeyValuePair<Node, Edge> kvPair in Root.OutgoingEdges)
            {
                Node node = kvPair.Key;
                if (Region.Instance.ObsoleteNodes.Contains(node.NodeID))
                {
                    continue;
                }
                if (Parent != null && Parent.NodeID == node.NodeID) // avoid cyclic relations
                {
                    continue;
                }

                //PredictiveNode child = new PredictiveNode(RoadNetwork, node, cost: kvPair.Value.Cost, depth: Depth - 1,
                //                                          maxDepth: MaxDepth, Root, PredictiveRegions);
                PredictiveNode child = new PredictiveNode(RoadNetwork, node, cost: kvPair.Value.Cost, depth: Depth - 1,
                                          maxDepth: MaxDepth, Root, PredictiveRegions);
                double distance = kvPair.Value.Distance;
                if (!WeightedChildren.ContainsKey(distance))
                {
                    WeightedChildren.Add(distance, child);
                }
                else
                {
                    WeightedChildren.Add(distance + 0.01, child);
                }
                Children.Add(child.Root.NodeID, child);
                Task task = Task.Factory.StartNew(() => child.Expand());
                taskList.Add(task);
            }
            Task.WaitAll(taskList.ToArray());
        }

        public double GetChildProbability(string nodeID)
        {
            if (!Children.ContainsKey(nodeID))
            {
                return 0;
            }
            double sumCost = 0;
            foreach (KeyValuePair<string, PredictiveNode> kv in Children)
            {
                sumCost += kv.Value.Cost;
            }
            double auxiliarySumCost = (Children.Count - 1) * sumCost;
            Children.TryGetValue(nodeID, out PredictiveNode node);
            if (sumCost == node.Cost)
            {
                node.Probability = 1;
                return 1;
            }

            double probability = (sumCost - node.Cost) * (1 / auxiliarySumCost);
            node.Probability = probability;
            return probability;
        }

        public RoadNetwork RoadNetwork { get; }
        public Node Root { get; private set; }
        public double Cost { get; }
        public int Depth { get; private set; }
        public int MaxDepth { get; }
        public int Level { get; }
        public ConcurrentDictionary<int, ConcurrentDictionary<string, List<PredictiveNode>>> PredictiveRegions { get; }
        public SortedDictionary<double, PredictiveNode> WeightedChildren { get; private set; }
        public Dictionary<string, PredictiveNode> Children { get; private set; }

        public double Probability = 0;

        public Node Parent = null;
    }

}
