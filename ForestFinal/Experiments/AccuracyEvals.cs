using CsvHelper;
using ForestFinal.Forest;
using ForestFinal.Util;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

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
            //PredictiveEvalsConstantRegionFinal();
            //PresentEvalsFinal();
            //HistoricalEvalsFinal();
            //TestPbar();
        }

        private void HistoricalEvalsFinal()
        {
            ProgressBarOptions options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkYellow,
                ProgressCharacter = '─',
                DisplayTimeInRealTime = true
            };
            List<dynamic> results = new List<dynamic>();

            using (ProgressBar regionSizeBar = new ProgressBar((int)Math.Ceiling(Parameters.MaxRegionSize) / (int)Math.Ceiling(Parameters.RegionSizeIncrement), "Region Size", options))
            {
                for (double regionSize = Parameters.MinRegionSize; regionSize <= Parameters.MaxRegionSize; regionSize += Parameters.RegionSizeIncrement)
                {
                    using (ChildProgressBar objectsBar = regionSizeBar.Spawn(MovingObjects.Count, "Moving Object", options))
                    {
                        int objectCount = 0;
                        foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
                        {
                            objectCount += 1;
                            Region.GetInstance().Reset(); // reset the region buffer for each object
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
                            var maxTrajectory = Math.Min(movingObject.Trajectory.Count, 9);
                            for (int update = 0; update < maxTrajectory; update++)
                            {
                                Node n = movingObject.Trajectory[update];
                                if (n == null)
                                {
                                    break;
                                }
                                forest.Update(n.Location, regionSize);

                                var predictedNodes = forest.MRegion.GetHistoricalProbabilities(0);
                                PresentResult result = new PresentResult
                                {
                                    TripID = movingObject.TripID,
                                    Update = update,
                                    RegionSize = regionSize
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
            ProgressBarOptions options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkYellow,
                ProgressCharacter = '─',
                DisplayTimeInRealTime = true
            };
            List<dynamic> results = new List<dynamic>();

            using (ProgressBar regionSizeBar = new ProgressBar((int)Math.Ceiling(Parameters.MaxRegionSize) / (int)Math.Ceiling(Parameters.RegionSizeIncrement), "Region Size", options))
            {
                for (double regionSize = Parameters.MinRegionSize; regionSize <= Parameters.MaxRegionSize; regionSize += Parameters.RegionSizeIncrement)
                {
                    using (ChildProgressBar objectsBar = regionSizeBar.Spawn(MovingObjects.Count, "Moving Object", options))
                    {
                        int objectCount = 0;
                        foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
                        {
                            objectCount += 1;
                            Region.GetInstance().Reset(); // reset the region buffer for each object
                            MovingObject movingObject = kv.Value;
                            if (!movingObject.HasValidTrajectory)
                            {
                                continue;
                            }
                            PredictiveForest forest = new PredictiveForest(RoadNetwork, 1);
                            var maxTrajectory = Math.Min(movingObject.Trajectory.Count, 9);
                            for (int update = 0; update < maxTrajectory; update++)
                            {
                                Node n = movingObject.Trajectory[update];
                                if (n == null)
                                {
                                    break;
                                }
                                forest.Update(n.Location, regionSize);

                                var predictedNodes = forest.MRegion.GetHistoricalProbabilities(update);
                                PresentResult result = new PresentResult
                                {
                                    TripID = movingObject.TripID,
                                    Update = update,
                                    RegionSize = regionSize
                                };
                                if (predictedNodes.TryGetValue(n.NodeID, out double probability))
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
            ProgressBarOptions options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkYellow,
                ProgressCharacter = '─',
                DisplayTimeInRealTime = true
            };
            List<dynamic> results = new List<dynamic>();
            using (ProgressBar predictiveDepthBar = new ProgressBar(Parameters.MaxPredictiveDepth / Parameters.PredictiveDepthIncrement, "Predictive Depth", options))
            {
                for (int predictiveDepth = Parameters.MinPredictiveDepth; predictiveDepth <= Parameters.MaxPredictiveDepth; predictiveDepth += Parameters.PredictiveDepthIncrement)
                {
                    using (ChildProgressBar regionSizeBar = predictiveDepthBar.Spawn((int)Math.Ceiling(Parameters.MaxRegionSize) / (int)Math.Ceiling(Parameters.RegionSizeIncrement), "Region Size", options))
                    {
                        for (double regionSize = Parameters.MinRegionSize; regionSize <= Parameters.MaxRegionSize; regionSize += Parameters.RegionSizeIncrement)
                        {
                            Dictionary<string, MovingObject> filteredObjects = FilterObjects(predictiveDepth);
                            using (ChildProgressBar objectsBar = regionSizeBar.Spawn(filteredObjects.Count, "Moving Object", options))
                            {
                                int objectCount = 0;
                                foreach (KeyValuePair<string, MovingObject> kv in filteredObjects)
                                {
                                    objectCount++;
                                    MovingObject movingObject = kv.Value;
                                    Region.GetInstance().Reset(); // reset the region buffer for each object

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
                                        PredictiveResult result = new PredictiveResult();
                                        result.TripID = movingObject.TripID;
                                        result.Update = update;
                                        if (predictedNodes == null)
                                        {
                                            continue;
                                        }
                                        if (predictedNodes.TryGetValue(predictiveNode.NodeID, out double probability))
                                        {
                                            result.Accuracy = probability;
                                        }
                                        else
                                        {
                                            result.Accuracy = 0;
                                        }
                                        result.PredictiveDepth = predictiveDepth;
                                        result.RegionSize = regionSize;
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

        private Dictionary<string, MovingObject> FilterObjects(int predictiveDepth)
        {
            Dictionary<string, MovingObject> filteredObjects = new Dictionary<string, MovingObject>();
            foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
            {
                MovingObject movingObject = kv.Value;
                if (!movingObject.HasValidTrajectory || movingObject.Trajectory.Count < predictiveDepth + 3)
                {
                    continue;
                }
                filteredObjects.Add(kv.Key, movingObject);
            }
            return filteredObjects;
        }

        #region fields
        public RoadNetwork RoadNetwork { get; }
        public Dictionary<string, MovingObject> MovingObjects { get; }
        #endregion

        #region testing
        public void TestPbar()
        {
            const int totalTicks = 100;
            ProgressBarOptions options = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkYellow,
                ProgressCharacter = '─',
                DisplayTimeInRealTime = false
            };
            ProgressBarOptions childOptions = new ProgressBarOptions
            {
                ForegroundColor = ConsoleColor.Green,
                BackgroundColor = ConsoleColor.DarkGreen,
                ProgressCharacter = '─',

            };
            using (ProgressBar pbar = new ProgressBar(totalTicks, "main progressbar", options))
            {
                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(50);
                    pbar.Tick();
                    using (ChildProgressBar child = pbar.Spawn(totalTicks, "child actions", childOptions))
                    {
                        for (int j = 0; j < 100; j++)
                        {
                            Thread.Sleep(50);
                            child.Tick();
                        }
                    }
                };
            }
        }

        private void PresentEvals()
        {
            foreach (KeyValuePair<string, MovingObject> kv in MovingObjects)
            {
                Region.GetInstance().Reset(); // reset the region buffer for each object
                MovingObject movingObject = kv.Value;
                if (!movingObject.HasValidTrajectory || movingObject.Trajectory.Count < 3 + 5)
                {
                    continue;
                }
                PredictiveForest forest = new PredictiveForest(RoadNetwork, 3);

                for (int i = 0; i < movingObject.Trajectory.Count; i++)
                {
                    Node n = movingObject.Trajectory[i];
                    forest.Update(n.Location, 500);

                    double probability = forest.MRegion.GetHistoricalProbability(i, n.NodeID);
                    Console.WriteLine($"Present Probability for trip {movingObject.TripID} for region {i} after {i} steps is {probability}");
                    Console.ReadKey();
                }
                Console.WriteLine($"Finished building forest for object {movingObject.TripID}");
            }
        }

        #endregion
    }
}
