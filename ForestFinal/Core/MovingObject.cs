using System;
using System.Collections;
using System.Collections.Generic;

namespace ForestFinal
{
    [Serializable()]
    internal class MovingObject
    {
        public MovingObject(string id, List<Edge> trajectoryEdges, Dictionary<Edge, double> costs, RoadNetwork roadNetwork)
        {
            TripID = id;
            TrajectoryEdges = trajectoryEdges;
            EdgeCosts = costs;
            RoadNetwork = roadNetwork;

            Trajectory = new List<Node>();
            NodalCosts = new Dictionary<string, double>();
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
                    if (EdgeCosts.TryGetValue(edge, out var _cost))
                    {
                        NodalCosts.Add(prevDestination.NodeID, _cost);
                    }
                    continue;
                }
                if (edge.Source != prevDestination)
                {
                    HasValidTrajectory = false;
                    return;
                }
                Trajectory.Add(prevDestination);
                
                prevDestination = edge.Destination;
                if (EdgeCosts.TryGetValue(edge, out var cost))
                {
                    try
                    {
                        NodalCosts.Add(prevDestination.NodeID, cost);
                    }
                    catch
                    {
                        HasValidTrajectory = false;
                        return;
                    }
                }
            }
            Trajectory.Add(prevDestination);
        }

        public List<Node> Trajectory;
        public string TripID { get; }
        public List<Edge> TrajectoryEdges { get; }
        private Dictionary<Edge, double> EdgeCosts { get; }
        public Dictionary<string, double> NodalCosts { get; }

        public RoadNetwork RoadNetwork { get; }
        public bool HasValidTrajectory { get; private set; }

        public static Dictionary<string, MovingObject> FilterObjects(Dictionary<string, MovingObject> movingObjects, int predictiveDepth)
        {
            Dictionary<string, MovingObject> filteredObjects = new Dictionary<string, MovingObject>();
            foreach (KeyValuePair<string, MovingObject> kv in movingObjects)
            {
                MovingObject movingObject = kv.Value;
                if (!movingObject.HasValidTrajectory || movingObject.Trajectory.Count < predictiveDepth + 1)
                {
                    continue;
                }
                filteredObjects.Add(kv.Key, movingObject);
            }
            return filteredObjects;
        }
    }
}
