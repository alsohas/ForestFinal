using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Device.Location;

namespace ForestFinal.Forest
{
    [Serializable()]
    public class PredictiveForest
    {
        public PredictiveForest(RoadNetwork roadNetwork, int depth)
        {
            SetRoadNetwork(roadNetwork);
            Depth = depth;
            MRegion = Region.Instance;
        }

        public void Update(GeoCoordinate center, double radius)
        {
            if (CurrentStep == 0)
            {
                MRegion.Update(GetRoadNetwork().GetNodesWithinRange(center, radius));
                MRegion.Regions.TryGetValue(CurrentStep, out ConcurrentDictionary<string, RegionalNode> region);
                ExpandPredictiveTrees(region);
                CurrentStep += 1;
                return;
            }

            // retrieve all nodes from previous region
            MRegion.Regions.TryGetValue(CurrentStep - 1, out ConcurrentDictionary<string, RegionalNode> pastNodes);

            // gathering child nodes from previous region
            HashSet<string> children = new HashSet<string>();
            foreach (KeyValuePair<string, RegionalNode> kv in pastNodes)
            {
                children.UnionWith(kv.Value.Children);
            }

            // gathering new nodes within latest region
            HashSet<string> currentNodes = new HashSet<string>();
            foreach (Node n in GetRoadNetwork().GetNodesWithinRange(center, radius))
            {
                currentNodes.Add(n.NodeID);
            }

            // only keep the nodes from newest region which intersects the previous region's children
            currentNodes.IntersectWith(children);

            // pruning children from the nodes of previous region
            // for each node in previous region, intersect its set of children with current nodes
            // if the resulting set is empty, the previous node is obsolete
            HashSet<string> obsoleteParents = new HashSet<string>();
            HashSet<string> validParents = new HashSet<string>();
            foreach (KeyValuePair<string, RegionalNode> kv in pastNodes)
            {
                RegionalNode pastNode = kv.Value;
                pastNode.Children.IntersectWith(currentNodes);
                if (pastNode.Children.Count == 0)
                {
                    obsoleteParents.Add(pastNode.NodeID);
                    continue;
                }
                validParents.Add(pastNode.NodeID);
            }

            /// adding all valid nodes to the latest region
            /// Note: the first region is initialized in <see cref="Region.Update(IEnumerable{Node})"/>
            ///
            ConcurrentDictionary<string, RegionalNode> newRegion = new ConcurrentDictionary<string, RegionalNode>();
            foreach (string nodeID in currentNodes) // note that current node has been cleared of all dead-end nodes
            {
                GetRoadNetwork().Nodes.TryGetValue(nodeID, out Node node);
                RegionalNode currentNode = new RegionalNode(node, parents: validParents);
                newRegion.TryAdd(nodeID, currentNode);
            }

            // add newest region to the Region buffer
            // make sure the new region is added to buffer before pruning obsolete parents
            // because the pruning function needs reference to this new region
            MRegion.Regions.TryAdd(CurrentStep, newRegion);
            PruneRegions(CurrentStep - 1, obsoleteParents);


            ExpandPredictiveTrees(newRegion); // Populate and expand predictive trees for new region
            CurrentStep += 1; // increment how many steps we've received updates from
        }

        internal void Update(GeoCoordinate center, double regionSize, double cost)
        {
            MRegion.AddCost(CurrentStep, cost);
            Update(center, regionSize);
        }

        /// <summary>
        /// Prunes nodes from the previous region, if at the top most region, removes nodes entirely
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="obsoleteNodes"></param>
        /// <returns></returns>
        private int PruneRegions(int steps, HashSet<string> obsoleteNodes)
        {
            MRegion.ObsoleteNodes.UnionWith(obsoleteNodes);
            if (obsoleteNodes.Count == 0)
            {
                return steps;
            }

            HashSet<string> obsoleteParents = new HashSet<string>();

            MRegion.Regions.TryGetValue(steps, out ConcurrentDictionary<string, RegionalNode> region); // get all nodes from the specified region
            MRegion.Regions.TryGetValue(steps - 1, out ConcurrentDictionary<string, RegionalNode> parentalRegion); // get all nodes from parental region

            foreach (string nodeID in obsoleteNodes)
            {
                region.TryGetValue(nodeID, out RegionalNode node);
                region.TryRemove(nodeID, out _); // remove the node from region after getting its parents
                HashSet<string> parents = node.Parents;
                if (parents == null)
                {
                    continue;
                }
                foreach (string pNodeID in parents)
                {
                    parentalRegion.TryGetValue(pNodeID, out RegionalNode parent);
                    parent.Children.Remove(nodeID);
                    if (parent.Children.Count == 0)
                    {
                        obsoleteParents.Add(pNodeID);
                    }
                }
            }
            return PruneRegions(steps - 1, obsoleteParents);
        }

        private void ExpandPredictiveTrees(ConcurrentDictionary<string, RegionalNode> newRegion)
        {
            PredictiveRegions = new ConcurrentDictionary<int, ConcurrentDictionary<string, List<PredictiveNode>>>();
            foreach (KeyValuePair<string, RegionalNode> kv in newRegion)
            {
                if (MRegion.ObsoleteNodes.Contains(kv.Value.NodeID))
                {
                    continue;
                }
                GetRoadNetwork().Nodes.TryGetValue(kv.Value.NodeID, out Node node);
                PredictiveNode predictiveNode = new PredictiveNode(GetRoadNetwork(), node, cost: 0, depth: Depth, maxDepth: Depth, null,
                                                                   PredictiveRegions);
                predictiveNode.Expand();
            }
        }

        #region predictive
        internal Dictionary<string, double> PredictNodes(int step)
        {
            double sumCost = 0;
            int nodeCount = 0;
            Dictionary<string, double> costs = GenerateNodeCosts(step, ref nodeCount);
            if (costs == null)
            {
                return null;
            }
            foreach (KeyValuePair<string, double> kv in costs)
            {
                sumCost += kv.Value;
            }

            Dictionary<string, double> newCosts = new Dictionary<string, double>();
            foreach (KeyValuePair<string, double> kv in costs)
            {
                double probability = (kv.Value) * (1 / sumCost);
                //Debug.Assert(probability >= 0);
                newCosts.Add(kv.Key, probability);
            }
            return newCosts;
        }

        private Dictionary<string, List<PredictiveNode>> GetPossibleChildren(int step)
        {
            if (step == 0)
            {
                return null;
            }

            PredictiveRegions.TryGetValue(step - 1, out ConcurrentDictionary<string, List<PredictiveNode>> previousRegion);
            if (previousRegion == null)
            {
                return null;
            }
            Dictionary<string, List<PredictiveNode>> possibleChildren = new Dictionary<string, List<PredictiveNode>>();
            foreach (KeyValuePair<string, List<PredictiveNode>> kv in previousRegion)
            {
                List<PredictiveNode> nodeList = kv.Value;
                foreach (PredictiveNode node in nodeList)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    foreach (KeyValuePair<double, PredictiveNode> _kv in node.WeightedChildren)
                    {
                        PredictiveNode child = _kv.Value;
                        possibleChildren.TryGetValue(child.Root.NodeID, out List<PredictiveNode> possibleChildrenList);
                        if (possibleChildrenList == null)
                        {
                            possibleChildrenList = new List<PredictiveNode>();
                            possibleChildren.Add(child.Root.NodeID, possibleChildrenList);
                        }
                        possibleChildrenList.Add(child);
                        child.Probability = node.GetChildProbability(child.Root.NodeID);
                    }
                }
            }
            return possibleChildren;
        }

        private Dictionary<string, double> GenerateNodeCosts(int step, ref int nodeCount)
        {
            Dictionary<string, List<PredictiveNode>> possibleChildren = GetPossibleChildren(step);
            if (possibleChildren == null)
            {
                return null;
            }
            Dictionary<string, double> costs = new Dictionary<string, double>();
            foreach (KeyValuePair<string, List<PredictiveNode>> kv in possibleChildren)
            {
                double localSum = 0;
                foreach (PredictiveNode node in kv.Value)
                {
                    nodeCount += 1;
                    localSum += node.Probability;
                }
                bool nodeExists = costs.TryGetValue(kv.Key, out double previousCosts);
                if (!nodeExists)
                {
                    costs.Add(kv.Key, localSum);
                }
                else
                {
                    costs[kv.Key] = previousCosts + localSum;
                }
            }
            return costs;
        }

        #endregion

        public ConcurrentDictionary<int, ConcurrentDictionary<string, List<PredictiveNode>>> PredictiveRegions;
        [NonSerialized()]
        private RoadNetwork roadNetwork;

        public RoadNetwork GetRoadNetwork()
        {
            return roadNetwork;
        }

        private void SetRoadNetwork(RoadNetwork value)
        {
            roadNetwork = value;
        }

        public int Depth { get; private set; }
        public Region MRegion { get; private set; }

        public int CurrentStep = 0;
    }
}
