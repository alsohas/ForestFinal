using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForestFinal.Forest
{
    [Serializable()]
    public class RegionalNode
    {
        public RegionalNode(Node node, HashSet<string> parents)
        {
            Probability = 0;
            Node = node;
            NodeID = node.NodeID;
            Children = new HashSet<string>();
            Parents = new HashSet<string>();

            List<Task> taskList = new List<Task>();
            Task parentsTask = Task.Factory.StartNew(() => AddParents(node, parents));
            taskList.Add(parentsTask);
            Task childrenTask = Task.Factory.StartNew(() => AddChildren(node));
            taskList.Add(childrenTask);

            Task.WaitAll(taskList.ToArray());
        }

        private void AddChildren(Node node)
        {
            foreach (KeyValuePair<Node, Edge> kv in node.OutgoingEdges)
            {
                Children.Add(kv.Key.NodeID);
            }
        }

        private void AddParents(Node node, HashSet<string> parents)
        {
            if (parents == null) // this would imply we're in the first region therefore no parents
            {
                return;
            }
            foreach (KeyValuePair<Node, Edge> kv in node.IncomingEdges)
            {
                string nodeID = kv.Key.NodeID;
                if (parents.Contains(nodeID)) // only add valid parents
                {
                    Parents.Add(kv.Key.NodeID);
                    Probability += kv.Value.Cost;
                }
            }
        }

        public double Probability { get; set; }
        public Node Node { get; }
        public string NodeID { get; private set; }
        public HashSet<string> Children;
        public HashSet<string> Parents;

    }
}
