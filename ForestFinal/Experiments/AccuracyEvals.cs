using ForestFinal.Forest;
using System;
using System.Collections.Generic;
namespace ForestFinal.Experiments
{
    internal class AccuracyEvals
    {
        public AccuracyEvals(RoadNetwork roadNetwork, Dictionary<string, MovingObject> movingObjects)
        {
            RoadNetwork = roadNetwork;
            MovingObjects = movingObjects;
            MaxDepth = Parameters.PredictiveDepth;
            RegionSize = Parameters.RegionSize;
        }

        public void Start()
        {
            //HistoricalEvals();
            //PredictiveEvals();
            PredictiveEvalsConstantRegion();
            //PredictiveEvalsVariableRegion();
            //PresentEvals();
        }

        private void PredictiveEvalsConstantRegion()
        {
            foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
            {
                Region.GetInstance().Reset(); // reset the region buffer for each object
                MovingObject movingObject = kv.Value;
                if (!movingObject.HasValidTrajectory || movingObject.Trajectory.Count < 6 * 2)
                {
                    continue;
                }
                PredictiveForest forest = new PredictiveForest(RoadNetwork, 6);

                int predictiveIndex = 6;
                Node predictiveNode;

                for (int i = 0; i < movingObject.Trajectory.Count; i++)
                {
                    try
                    {
                        predictiveNode = movingObject.Trajectory[predictiveIndex];
                    }
                    catch
                    {
                        break;
                    }
                    Node n = movingObject.Trajectory[i];
                    forest.Update(n.Location, RegionSize);
                    if (predictiveIndex == i)
                    {
                        break;
                    }
                    var predictedNodes = forest.PredictNodes(predictiveIndex - i);
                    Console.WriteLine($"Predicted the following for trip {movingObject.TripID} for region {predictiveIndex} after step {i}");
                    Console.WriteLine($"Node traveled to: {predictiveNode.NodeID}");
                    if (predictedNodes == null)
                    {
                        continue;
                    }
                    try
                    {
                        Console.WriteLine($"Probability: {predictedNodes[predictiveNode.NodeID]}");
                    }
                    catch
                    {
                        Console.WriteLine($"Probability: 0");
                    }
                    Console.ReadKey();
                }
                Console.WriteLine($"Finished building forest for object {movingObject.TripID}");
                Console.WriteLine("----------------------------------------");
                Console.ReadKey();

            }
        }

        private void PredictiveEvalsVariableRegion()
        {
            foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
            {
                Region.GetInstance().Reset(); // reset the region buffer for each object
                MovingObject movingObject = kv.Value;
                if (!movingObject.HasValidTrajectory || movingObject.Trajectory.Count < MaxDepth * 2)
                {
                    continue;
                }
                PredictiveForest forest = new PredictiveForest(RoadNetwork, MaxDepth);

                int predictiveIndex = 1;
                Node predictiveNode;

                for (int i = 0; i < movingObject.Trajectory.Count; i++)
                {
                    try
                    {
                        predictiveNode = movingObject.Trajectory[predictiveIndex + i];
                    }
                    catch
                    {
                        break;
                    }
                    Node n = movingObject.Trajectory[i];
                    forest.Update(n.Location, RegionSize);
                    //if (predictiveIndex == i)
                    //{
                    //    break;
                    //}
                    var predictedNodes = forest.PredictNodes(predictiveIndex);
                    Console.WriteLine($"Predicted the following for trip {movingObject.TripID} for region {predictiveIndex} after step {i}");
                    Console.WriteLine($"Node traveled to: {predictiveNode.NodeID}");
                    if (predictedNodes == null)
                    {
                        continue;
                    }
                    try
                    {
                        Console.WriteLine($"Probability: {predictedNodes[predictiveNode.NodeID]}");
                    }
                    catch
                    {
                        Console.WriteLine($"Probability: 0");
                        }
                    Console.ReadKey();
                }
                Console.WriteLine($"Finished building forest for object {movingObject.TripID}");
                Console.WriteLine("----------------------------------------");
                Console.ReadKey();

            }
        }

        private void PresentEvals()
        {
            foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
            {
                Region.GetInstance().Reset(); // reset the region buffer for each object
                MovingObject movingObject = kv.Value;
                if (!movingObject.HasValidTrajectory || movingObject.Trajectory.Count < MaxDepth + 5)
                {
                    continue;
                }
                PredictiveForest forest = new PredictiveForest(RoadNetwork, MaxDepth);

                for (int i = 0; i < movingObject.Trajectory.Count; i++)
                {
                    Node n = movingObject.Trajectory[i];
                    forest.Update(n.Location, RegionSize);

                    double probability = forest.MRegion.GetHistoricalProbability(i, n.NodeID);
                    Console.WriteLine($"Present Probability for trip {movingObject.TripID} for region {i} after {i} steps is {probability}");
                    Console.ReadKey();
                }
                Console.WriteLine($"Finished building forest for object {movingObject.TripID}");
            }
        }

        private void HistoricalEvals()
        {
            foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
            {
                Region.GetInstance().Reset(); // reset the region buffer for each object
                MovingObject movingObject = kv.Value;
                if (!movingObject.HasValidTrajectory || movingObject.Trajectory.Count < MaxDepth + 10)
                {
                    continue;
                }
                PredictiveForest forest = new PredictiveForest(RoadNetwork, MaxDepth);

                int predictiveIndex = 2;
                Node predictiveNode = movingObject.Trajectory[predictiveIndex];

                for (int i = 0; i < movingObject.Trajectory.Count; i++)
                {
                    Node n = movingObject.Trajectory[i];
                    forest.Update(n.Location, RegionSize);

                    double probability = forest.MRegion.GetHistoricalProbability(predictiveIndex, predictiveNode.NodeID);
                    Console.WriteLine($"Historical Probability for trip {movingObject.TripID} for region {predictiveIndex} after {i} steps is {probability}");
                    Console.ReadKey();
                }
                Console.WriteLine($"Finished building forest for object {movingObject.TripID}");
            }
        }

        public RoadNetwork RoadNetwork { get; }
        public Dictionary<string, MovingObject> MovingObjects { get; }
        public int MaxDepth { get; }
        public double RegionSize { get; }
    }
}
