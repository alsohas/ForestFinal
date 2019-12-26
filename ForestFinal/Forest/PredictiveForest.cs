using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;

namespace ForestFinal.Forest
{
    public class PredictiveForest
    {
        public PredictiveForest(RoadNetwork roadNetwork, int depth)
        {
            RoadNetwork = roadNetwork;
            Depth = depth;
            MRegion = Region.GetInstance();
        }

        public void Update(GeoCoordinate center, double radius)
        {
            if (CurrentStep == 0)
            {
                MRegion.Update(RoadNetwork.GetNodesWithinRange(center, radius));
                MRegion.Regions.TryGetValue(CurrentStep, out var region);
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
            foreach (Node n in RoadNetwork.GetNodesWithinRange(center, radius))
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

            // adding all valid nodes to the latest region
            ConcurrentDictionary<string, RegionalNode> newRegion = new ConcurrentDictionary<string, RegionalNode>();
            foreach (string nodeID in currentNodes) // note that current node has been cleared of all dead-end nodes
            {
                RoadNetwork.Nodes.TryGetValue(nodeID, out Node node);
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

        /// <summary>
        /// Prunes nodes from the previous region, if at the top most region, removes nodes entirely
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="obsoleteNodes"></param>
        /// <returns></returns>
        private int PruneRegions(int steps, HashSet<string> obsoleteNodes)
        {
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
                RoadNetwork.Nodes.TryGetValue(kv.Value.NodeID, out Node node);
                PredictiveNode predictiveNode = new PredictiveNode(RoadNetwork, node, cost: 0, depth: Depth, maxDepth: Depth, null,
                                                                   PredictiveRegions);
                predictiveNode.Expand();
            }
        }
        
        #region predictive
        internal Dictionary<string, double> PredictNodes(int step)
        {
            double sumCost = 0;
            int nodeCount = 0;
            var costs = GenerateNodeCosts(step, ref nodeCount);
            if (costs == null)
            {
                return null;
            }
            foreach (var kv in costs)
            {
                sumCost += kv.Value;
            }

            var newCosts = new Dictionary<string, double>();
            foreach (var kv in costs)
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

            PredictiveRegions.TryGetValue(step - 1, out var previousRegion);
            if (previousRegion == null)
            {
                return null;
            }
            var possibleChildren = new Dictionary<string, List<PredictiveNode>>();
            foreach (var kv in previousRegion)
            {
                var nodeList = kv.Value;
                foreach (var node in nodeList)
                {
                    foreach (var _kv in node.WeightedChildren)
                    {
                        var child = _kv.Value;
                        possibleChildren.TryGetValue(child.Root.NodeID, out var possibleChildrenList);
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
            var costs = new Dictionary<string, double>();
            foreach (var kv in possibleChildren)
            {
                double localSum = 0;
                foreach (var node in kv.Value)
                {
                    nodeCount += 1;
                    localSum += node.Probability;
                }
                var nodeExists = costs.TryGetValue(kv.Key, out var previousCosts);
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
        public RoadNetwork RoadNetwork { get; private set; }
        public int Depth { get; private set; }
        public Region MRegion { get; private set; }
        public int CurrentStep = 0;
    }
}
