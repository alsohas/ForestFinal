using System.Collections.Generic;

namespace ForestFinal
{
    internal class MovingObject
    {
        public MovingObject(string id, List<Edge> trajectoryEdges, Dictionary<Edge, double> costs, RoadNetwork roadNetwork)
        {
            TripID = id;
            TrajectoryEdges = trajectoryEdges;
            Costs = costs;
            RoadNetwork = roadNetwork;

            Trajectory = new List<Node>();
            VerifyTrajectory();
        }

        private void VerifyTrajectory()
        {
            HasValidTrajectory = true;
            Node prevDestination = null;
            foreach (Edge edge in TrajectoryEdges)
            {
                if (prevDestination == null)
                {
                    prevDestination = edge.Destination;
                    Trajectory.Add(edge.Source);
                    continue;
                }
                if (edge.Source != prevDestination)
                {
                    HasValidTrajectory = false;
                    return;
                }
                Trajectory.Add(prevDestination);
                prevDestination = edge.Destination;
            }
            Trajectory.Add(prevDestination);
        }

        public List<Node> Trajectory;
        public string TripID { get; }
        public List<Edge> TrajectoryEdges { get; }
        public Dictionary<Edge, double> Costs { get; }
        public RoadNetwork RoadNetwork { get; }
        public bool HasValidTrajectory { get; private set; }
    }
}
