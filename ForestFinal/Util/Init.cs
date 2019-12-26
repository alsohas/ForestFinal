using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace ForestFinal.Util
{
    internal class Init
    {
        public static void Initialize(RoadNetwork roadNetwork, Dictionary<string, MovingObject> movingObjects)
        {
            roadNetwork.BuildNetwork();
            ReadMovingObjects(roadNetwork, movingObjects);
        }

        private static void ReadMovingObjects(RoadNetwork roadNetwork, Dictionary<string, MovingObject> movingObjects)
        {
            string jsonString = File.ReadAllText(Parameters.TripsFile);
            JObject trips = JObject.Parse(jsonString);
            foreach (KeyValuePair<string, JToken> trip in trips)
            {
                string tripID = trip.Key;
                JToken trajectory = trip.Value;

                List<Edge> parsedTrajectory = new List<Edge>();
                Dictionary<Edge, double> costs = new Dictionary<Edge, double>();


                foreach (dynamic point in trajectory)
                {
                    string source = point.source.ToString();
                    string destination = point.destination.ToString();
                    double cost = double.Parse(point.cost.ToString());

                    bool edgeExists = roadNetwork.Edges.TryGetValue((source, destination), out Edge parsedEdge);
                    if (!edgeExists)
                    {
                        break;
                    }
                    bool edgeVisited = costs.ContainsKey(parsedEdge);

                    if (edgeVisited) // Avoid adding edges that lead to cyclic trajectories
                    {
                        continue;
                    }
                    costs.Add(parsedEdge, cost);
                    parsedTrajectory.Add(parsedEdge);
                }
                MovingObject movingObject = new MovingObject(tripID, parsedTrajectory, costs, roadNetwork);
                movingObjects.Add(tripID, movingObject);
            }
        }
    }
}
