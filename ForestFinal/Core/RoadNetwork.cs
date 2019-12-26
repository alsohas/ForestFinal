using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Device.Location;
using System.IO;

namespace ForestFinal
{
    public class RoadNetwork
    {
        public Dictionary<string, Node> Nodes;
        public Dictionary<(string, string), Edge> Edges;
        public RoadNetwork()
        {
            Nodes = new Dictionary<string, Node>();
            Edges = new Dictionary<(string, string), Edge>();
        }

        internal IEnumerable<Node> GetNodesWithinRange(GeoCoordinate center, double radius)
        {
            foreach (KeyValuePair<string, Node> kv in Nodes)
            {
                if (center.GetDistanceTo(kv.Value.Location) <= radius)
                {
                    yield return kv.Value;
                }
            }
        }

        public void BuildNetwork()
        {
            InitializeNodes();
            InitializeEdges();
            BuildRoadNetwork();
        }

        private void BuildRoadNetwork()
        {
            foreach (KeyValuePair<(string, string), Edge> kv in Edges)
            {
                Edge edge = kv.Value;
                Node sourceNode = edge.Source;
                Node destinationNode = edge.Destination;
                sourceNode.AddOutgoingEdge(destinationNode, edge);
                destinationNode.AddIncomingEdge(sourceNode, edge);
            }
        }

        private void InitializeEdges()
        {
            string jsonString = File.ReadAllText(Parameters.EdgesFile);
            JArray edges = JArray.Parse(jsonString);
            foreach (dynamic edge in edges)
            {
                string source = edge.source.ToString();
                string destination = edge.destination.ToString();
                double time = double.Parse(edge.time.ToString());
                double distance = double.Parse(edge.distance.ToString());

                bool sourceExists = Nodes.TryGetValue(source, out Node sourceNode);
                bool destinationExists = Nodes.TryGetValue(destination, out Node destinationNode);

                if (!sourceExists || !destinationExists)
                {
                    continue;
                }
                Edge parsedEdge = new Edge(sourceNode, destinationNode, cost: time, distance: distance);
                if (!Edges.ContainsKey((source, destination)))
                {
                    Edges.Add((source, destination), parsedEdge);
                }
            }
        }
        private void InitializeNodes()
        {
            string jsonString = File.ReadAllText(Parameters.NodesFile);
            JArray nodes = JArray.Parse(jsonString);
            foreach (dynamic node in nodes)
            {
                dynamic id = node.id.ToString();
                double lng = node.coordinate.lng;
                double lat = node.coordinate.lat;
                Node parsed_node = new Node(id, lng, lat);
                Nodes.Add(id, parsed_node);
            }
        }
    }
}
