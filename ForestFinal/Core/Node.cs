using System;
using System.Collections.Generic;
using System.Device.Location;

namespace ForestFinal
{
    [Serializable()]
    public class Node
    {
        public string NodeID;
        [NonSerialized]
        public GeoCoordinate Location;

        public Dictionary<Node, Edge> IncomingEdges;
        public Dictionary<Node, Edge> OutgoingEdges;

        public Node(string nodeID, double lng, double lat)
        {
            NodeID = nodeID;
            Location = new GeoCoordinate(longitude: lng, latitude: lat);
            OutgoingEdges = new Dictionary<Node, Edge>();
            IncomingEdges = new Dictionary<Node, Edge>();
        }

        public void AddOutgoingEdge(Node node, Edge edge)
        {
            if (!OutgoingEdges.ContainsKey(node))
            {
                OutgoingEdges.Add(node, edge);
            }
            if (node.IncomingEdges.ContainsKey(this))
            {
                return;
            }
            node.AddIncomingEdge(this, edge);
        }

        public void AddIncomingEdge(Node node, Edge edge)
        {
            if (!IncomingEdges.ContainsKey(node))
            {
                IncomingEdges.Add(node, edge);
            }
            if (node.OutgoingEdges.ContainsKey(this))
            {
                return;
            }
            node.AddOutgoingEdge(this, edge);
        }
    }
}