using System;

namespace ForestFinal
{
    [Serializable()]
    public class Edge
    {
        public Node Source;
        public Node Destination;
        public double Cost;
        public double Distance;


        public Edge(Node source, Node destination, double cost, double distance)
        {
            Source = source;
            Destination = destination;
            Cost = cost;
            Distance = distance;
        }

    }
}