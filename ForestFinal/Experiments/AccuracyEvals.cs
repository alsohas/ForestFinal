using CsvHelper;
using ForestFinal.Forest;
using ForestFinal.Util;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ForestFinal.Experiments
{
    internal class AccuracyEvals
    {
        public AccuracyEvals(RoadNetwork roadNetwork, Dictionary<string, MovingObject> movingObjects)
        {
            RoadNetwork = roadNetwork;
            MovingObjects = movingObjects;
        }

        public void Start()
        {
            //if (Parameters.FuncID == 1)
            //{
            //    HistoricalEvalsFinal();
            //    return;
            //}
            //if (Parameters.FuncID == 2)
            //{
            //    PresentEvalsFinal();
            //    return;
            //}
            //if (Parameters.FuncID == 3)
            //{
            //    PredictiveEvalsConstantRegionFinal();
            //    return;
            //}
            //if (Parameters.FuncID == 4)
            //{
            //    PredictiveEvalsVariableRegionFinal();
            //    return;
            //}
            PredictiveEvalsConstantRegionFinal();

        }

        private void HistoricalEvalsFinal()
        {
            List<dynamic> results = new List<dynamic>();

            using (ProgressBar regionSizeBar = new ProgressBar((int)Math.Ceiling(Parameters.MaxRegionSize) / (int)Math.Ceiling(Parameters.RegionSizeIncrement), "Region Size", Options))
            {
                for (double regionSize = Parameters.MinRegionSize; regionSize <= Parameters.MaxRegionSize; regionSize += Parameters.RegionSizeIncrement)
                {
                    using (ChildProgressBar objectsBar = regionSizeBar.Spawn(MovingObjects.Count, "Moving Object", Options))
                    {
                        int objectCount = 0;
                        foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
                        {
                            objectCount += 1;
                            Region.Instance.Reset(); // reset the region buffer for each object
                            MovingObject movingObject = kv.Value;
                            if (!movingObject.HasValidTrajectory)
                            {
                                continue;
                            }
                            Node historicalNode = movingObject.Trajectory[0];
                            if (historicalNode == null)
                            {
                                continue;
                            }
                            PredictiveForest forest = new PredictiveForest(RoadNetwork, 1);
                            int maxTrajectory = Math.Min(movingObject.Trajectory.Count, 9);
                            for (int update = 0; update < maxTrajectory; update++)
                            {
                                Node n = movingObject.Trajectory[update];
                                if (n == null)
                                {
                                    break;
                                }
                                if (movingObject.NodalCosts.TryGetValue(n.NodeID, out var cost)) {
                                    forest.Update(n.Location, regionSize, cost);
                                } else
                                {
                                    forest.Update(n.Location, regionSize);
                                }

                                Dictionary<string, double> predictedNodes = forest.MRegion.GetHistoricalProbabilities(0);
                                if (predictedNodes == null)
                                {
                                    continue;
                                }
                                PresentResult result = new PresentResult
                                {
                                    TripID = movingObject.TripID,
                                    Update = update,
                                    RegionSize = regionSize,
                                    NodeCount = predictedNodes.Count
                                };
                                if (predictedNodes.TryGetValue(historicalNode.NodeID, out double probability))
                                {
                                    result.Accuracy = probability;
                                }
                                else
                                {
                                    result.Accuracy = 0;
                                }
                                results.Add(result);
                            }
                            objectsBar.Tick($"Moving Object {objectCount} out of {MovingObjects.Count}");
                        }
                    }
                    regionSizeBar.Tick($"Region Size {regionSize} out of {Parameters.MaxRegionSize}");
                }
            }
            using (StreamWriter streamWriter = new StreamWriter($"{Parameters.HistoricalAccuracyFile}", false, Encoding.UTF8))
            {
                using (CsvWriter csvWriter = new CsvWriter(streamWriter))
                {
                    csvWriter.WriteRecords(results);
                }
            }
        }

        internal void PresentEvalsFinal()
        {
            List<dynamic> results = new List<dynamic>();

            //using (ProgressBar regionSizeBar = new ProgressBar((int)Math.Ceiling(Parameters.MaxRegionSize) / (int)Math.Ceiling(Parameters.RegionSizeIncrement), "Region Size", Options))
            {
                for (double regionSize = Parameters.MinRegionSize; regionSize <= Parameters.MaxRegionSize; regionSize += Parameters.RegionSizeIncrement)
                {
                    //using (ChildProgressBar objectsBar = regionSizeBar.Spawn(MovingObjects.Count, "Moving Object", Options))
                    {
                        int objectCount = 0;
                        foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
                        {
                            objectCount += 1;
                            Region.Instance.Reset(); // reset the region buffer for each object
                            MovingObject movingObject = kv.Value;
                            if (!movingObject.HasValidTrajectory)
                            {
                                continue;
                            }
                            PredictiveForest forest = new PredictiveForest(RoadNetwork, 1);
                            int maxTrajectory = Math.Min(movingObject.Trajectory.Count, 9);
                            for (int update = 0; update < maxTrajectory; update++)
                            {
                                Node n = movingObject.Trajectory[update];
                                if (n == null)
                                {
                                    break;
                                }
                                forest.Update(n.Location, regionSize);

                                Dictionary<string, double> predictedNodes = forest.MRegion.GetHistoricalProbabilities(update);
                                Dictionary<string, double> predictedNodesWithCost = forest.MRegion.GetHistoricalProbabilitiesWithCost(update);

                                if (predictedNodes == null)
                                {
                                    continue;
                                }
                                PresentResult result = new PresentResult
                                {
                                    TripID = movingObject.TripID,
                                    Update = update,
                                    RegionSize = regionSize,
                                    NodeCount = predictedNodes.Count
                                };

                                if (predictedNodes.TryGetValue(n.NodeID, out double probability))
                                {
                                    result.Accuracy = probability;
                                    if (predictedNodesWithCost.TryGetValue(n.NodeID, out double costProbability))
                                    {
                                        Console.WriteLine($"Without cost: {probability}, With cost: {costProbability}");
                                    }
                                }
                                else
                                {
                                    result.Accuracy = 0;
                                }
                                Console.ReadKey();
                                results.Add(result);
                            }
                            //objectsBar.Tick($"Moving Object {objectCount} out of {MovingObjects.Count}");
                        }
                    }
                    //regionSizeBar.Tick($"Region Size {regionSize} out of {Parameters.MaxRegionSize}");
                }
            }
            using (StreamWriter streamWriter = new StreamWriter($"{Parameters.PresentAccuracyFile}", false, Encoding.UTF8))
            {
                using (CsvWriter csvWriter = new CsvWriter(streamWriter))
                {
                    csvWriter.WriteRecords(results);
                }
            }
        }

        internal void PredictiveEvalsConstantRegionFinal()
        {
            List<dynamic> results = new List<dynamic>();
            using (ProgressBar predictiveDepthBar = new ProgressBar(Parameters.MaxPredictiveDepth / Parameters.PredictiveDepthIncrement, "Predictive Depth", Options))
            {
                for (int predictiveDepth = Parameters.MinPredictiveDepth; predictiveDepth <= Parameters.MaxPredictiveDepth; predictiveDepth += Parameters.PredictiveDepthIncrement)
                {
                    using (ChildProgressBar regionSizeBar = predictiveDepthBar.Spawn((int)Math.Ceiling(Parameters.MaxRegionSize) / (int)Math.Ceiling(Parameters.RegionSizeIncrement), "Region Size", Options))
                    {
                        for (double regionSize = Parameters.MinRegionSize; regionSize <= Parameters.MaxRegionSize; regionSize += Parameters.RegionSizeIncrement)
                        {
                            Dictionary<string, MovingObject> filteredObjects = MovingObject.FilterObjects(MovingObjects, predictiveDepth);
                            using (ChildProgressBar objectsBar = regionSizeBar.Spawn(filteredObjects.Count, "Moving Object", Options))
                            {
                                int objectCount = 0;
                                foreach (KeyValuePair<string, MovingObject> kv in filteredObjects)
                                {
                                    objectCount++;
                                    MovingObject movingObject = kv.Value;
                                    Region.Instance.Reset(); // reset the region buffer for each object

                                    PredictiveForest forest = new PredictiveForest(RoadNetwork, predictiveDepth);
                                    Node predictiveNode = movingObject.Trajectory[predictiveDepth];
                                    for (int update = 0; update < movingObject.Trajectory.Count; update++)
                                    {
                                        if (update > regionSize)
                                        {
                                            break;
                                        }
                                        Node centralNode = movingObject.Trajectory[update];
                                        forest.Update(centralNode.Location, regionSize);

                                        Dictionary<string, double> predictedNodes = forest.PredictNodes(predictiveDepth - update);
                                        if (predictedNodes == null)
                                        {
                                            continue;
                                        }
                                        PredictiveResult result = new PredictiveResult
                                        {
                                            TripID = movingObject.TripID,
                                            Update = update,
                                            PredictiveDepth = predictiveDepth,
                                            RegionSize = regionSize,
                                            NodeCount = predictedNodes.Count,
                                        };

                                        if (predictedNodes.TryGetValue(predictiveNode.NodeID, out double probability))
                                        {
                                            result.Accuracy = probability;
                                        }
                                        else
                                        {
                                            result.Accuracy = 0;
                                        }

                                        results.Add(result);
                                    }
                                    objectsBar.Tick($"Moving Object {objectCount} out of {filteredObjects.Count}");
                                }
                            }
                            regionSizeBar.Tick($"Region Size {regionSize} out of {Parameters.MaxRegionSize}");
                        }
                    }
                    predictiveDepthBar.Tick($"Predictive Depth {predictiveDepth} out of {Parameters.MaxPredictiveDepth}");
                }
            }

            using (StreamWriter streamWriter = new StreamWriter($"{Parameters.PredictiveAccuracyFile}", false, Encoding.UTF8))
            {
                using (CsvWriter csvWriter = new CsvWriter(streamWriter))
                {
                    csvWriter.WriteRecords(results);
                }
            }
        }

        private double GetAccuracy(Dictionary<string, double> predictedNodes, string nodeID)
        {
            string maxProbabilityNode = "";
            double maxProbability = 0;
            foreach(var kv in predictedNodes)
            {
                if (kv.Value > maxProbability)
                {
                    maxProbability = kv.Value;
                    maxProbabilityNode = kv.Key;
                }
            }
            if (maxProbabilityNode == nodeID)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        internal void PredictiveEvalsVariableRegionFinal()
        {
            List<dynamic> results = new List<dynamic>();
            using (ProgressBar predictiveDepthBar = new ProgressBar(Parameters.MaxPredictiveDepth / Parameters.PredictiveDepthIncrement, "Predictive Depth", Options))
            {
                for (int predictiveDepth = Parameters.MinPredictiveDepth; predictiveDepth <= Parameters.MaxPredictiveDepth; predictiveDepth += Parameters.PredictiveDepthIncrement)
                {
                    using (ChildProgressBar regionSizeBar = predictiveDepthBar.Spawn((int)Math.Ceiling(Parameters.MaxRegionSize) / (int)Math.Ceiling(Parameters.RegionSizeIncrement), "Region Size", Options))
                    {
                        for (double regionSize = Parameters.MinRegionSize; regionSize <= Parameters.MaxRegionSize; regionSize += Parameters.RegionSizeIncrement)
                        {
                            Dictionary<string, MovingObject> filteredObjects = MovingObject.FilterObjects(MovingObjects, predictiveDepth + 6);
                            using (ChildProgressBar objectsBar = regionSizeBar.Spawn(filteredObjects.Count, "Moving Object", Options))
                            {
                                int objectCount = 0;
                                foreach (KeyValuePair<string, MovingObject> kv in filteredObjects)
                                {
                                    objectCount++;
                                    MovingObject movingObject = kv.Value;
                                    Region.Instance.Reset(); // reset the region buffer for each object

                                    PredictiveForest forest = new PredictiveForest(RoadNetwork, predictiveDepth);
                                    for (int update = 0; update < movingObject.Trajectory.Count; update++)
                                    {

                                        Node centralNode = movingObject.Trajectory[update];
                                        forest.Update(centralNode.Location, regionSize);

                                        int predictiveStep = predictiveDepth + update;
                                        if (predictiveStep >= movingObject.Trajectory.Count)
                                        {
                                            break;
                                        }
                                        Node predictiveNode = movingObject.Trajectory[predictiveStep];
                                        Dictionary<string, double> predictedNodes = forest.PredictNodes(predictiveDepth);
                                        if (predictedNodes == null)
                                        {
                                            continue;
                                        }
                                        PredictiveResult result = new PredictiveResult
                                        {
                                            TripID = movingObject.TripID,
                                            Update = update,
                                            PredictiveDepth = predictiveDepth,
                                            RegionSize = regionSize,
                                            NodeCount = predictedNodes.Count
                                        };
                                        
                                        if (predictedNodes.TryGetValue(predictiveNode.NodeID, out double probability))
                                        {
                                            result.Accuracy = probability;
                                        }
                                        else
                                        {
                                            result.Accuracy = 0;
                                        }

                                        results.Add(result);
                                    }
                                    objectsBar.Tick($"Moving Object {objectCount} out of {filteredObjects.Count}");
                                }
                            }
                            regionSizeBar.Tick($"Region Size {regionSize} out of {Parameters.MaxRegionSize}");
                        }
                    }
                    predictiveDepthBar.Tick($"Predictive Depth {predictiveDepth} out of {Parameters.MaxPredictiveDepth}");
                }
            }
            using (StreamWriter streamWriter = new StreamWriter($"{Parameters.ContinuousPredictiveAccuracyFile}", false, Encoding.UTF8))
            {
                using (CsvWriter csvWriter = new CsvWriter(streamWriter))
                {
                    csvWriter.WriteRecords(results);
                }
            }
        }

        #region fields
        public RoadNetwork RoadNetwork { get; }
        public Dictionary<string, MovingObject> MovingObjects { get; }

        ProgressBarOptions Options = new ProgressBarOptions
        {
            ForegroundColor = ConsoleColor.Yellow,
            BackgroundColor = ConsoleColor.DarkYellow,
            ProgressCharacter = '─',
            DisplayTimeInRealTime = false
        };
        #endregion
    }
}
