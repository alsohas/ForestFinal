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
                                forest.Update(n.Location, regionSize);

                                Dictionary<string, double> predictedNodes = forest.MRegion.GetHistoricalProbabilities(0);
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
                            Dictionary<string, MovingObject> filteredObjects = MovingObject.FilterObjects(MovingObjects, predictiveDepth);
                            using (ChildProgressBar objectsBar = regionSizeBar.Spawn(filteredObjects.Count, "Moving Object", options))
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
