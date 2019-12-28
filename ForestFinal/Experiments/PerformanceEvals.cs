using CsvHelper;
using ForestFinal.Forest;
using ForestFinal.Util;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace ForestFinal.Experiments
{
    internal class PerformanceEvals
    {
        public PerformanceEvals(RoadNetwork roadNetwork, Dictionary<string, MovingObject> movingObjects)
        {
            RoadNetwork = roadNetwork;
            MovingObjects = movingObjects;
        }

        public void Start()
        {
            PerformanceEvalsFinal();
            //PredictiveEvalsConstantRegionFinal();
            //PresentEvalsFinal();
            //HistoricalEvalsFinal();
            //TestPbar();
        }

        internal void PerformanceEvalsFinal()
        {

            using (ProgressBar predictiveDepthBar = new ProgressBar(Parameters.MaxPredictiveDepth / Parameters.PredictiveDepthIncrement, "Predictive Depth", Options))
            {
                for (int predictiveDepth = Parameters.MinPredictiveDepth; predictiveDepth <= Parameters.MaxPredictiveDepth; predictiveDepth += Parameters.PredictiveDepthIncrement)
                {
                    using (ChildProgressBar regionSizeBar = predictiveDepthBar.Spawn((int)Math.Ceiling(Parameters.MaxRegionSize) / (int)Math.Ceiling(Parameters.RegionSizeIncrement), "Region Size", Options))
                    {
                        for (double regionSize = Parameters.MinRegionSize; regionSize <= Parameters.MaxRegionSize; regionSize += Parameters.RegionSizeIncrement)
                        {
                            Dictionary<string, MovingObject> filteredObjects = MovingObject.FilterObjects(MovingObjects, predictiveDepth);
                            int maxObjectsCount = Math.Min(filteredObjects.Count, Parameters.Offset);
                            int minObjectsCount = Math.Max(0, Parameters.Offset - 1000);
                            using (ChildProgressBar objectsBar = regionSizeBar.Spawn(maxObjectsCount - minObjectsCount, "Moving Object", Options))
                            {
                                List<Task> taskList = new List<Task>();

                                int objectCount = 0;
                                for(int i = minObjectsCount; i < maxObjectsCount; i++)
                                {
                                    objectCount++;
                                    MovingObject movingObject = filteredObjects.Values.ElementAt(i);
                                    Task objectEvalTask = Task.Factory.StartNew(() => EvaluateObject(movingObject, regionSize, predictiveDepth, objectsBar, i, maxObjectsCount));
                                    taskList.Add(objectEvalTask);
                                    if (objectCount % 5 == 0)
                                    {
                                        Task.WaitAll(taskList.ToArray());
                                        taskList.Clear();
                                    }
                                }
                                Task.WaitAll(taskList.ToArray());
                            }
                            regionSizeBar.Tick($"Region Size {regionSize} out of {Parameters.MaxRegionSize}");
                        }
                    }
                    predictiveDepthBar.Tick($"Predictive Depth {predictiveDepth} out of {Parameters.MaxPredictiveDepth}");
                }
            }
            using (StreamWriter streamWriter = new StreamWriter($"{Parameters.Offset}{Parameters.PerformanceFile}", false, Encoding.UTF8))
            {
                using (CsvWriter csvWriter = new CsvWriter(streamWriter))
                {
                    csvWriter.WriteRecords(Results);
                }
            }
        }

        public void EvaluateObject(MovingObject movingObject, double regionSize, int predictiveDepth, ChildProgressBar objectsBar, int objectIndex, int totalObjects)
        {
            Region.Instance.Reset(); // reset the region buffer for each object
            PredictiveForest forest = new PredictiveForest(RoadNetwork, predictiveDepth);

            for (int update = 0; update < movingObject.Trajectory.Count; update++)
            {
                if (update > regionSize)
                {
                    break;
                }
                Node centralNode = movingObject.Trajectory[update];

                var timer = Stopwatch.StartNew();

                forest.Update(centralNode.Location, regionSize);

                #region resource usage calculation
                timer.Stop();

                var timeElapsed = 1_000.0 * (double)timer.ElapsedTicks / Stopwatch.Frequency;
                double memUsed = 0;
                using (Stream s = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(s, forest);
                    memUsed = s.Length / 1_000_000.0;
                }
                #endregion

                PerformanceResult result = new PerformanceResult
                {
                    TripID = movingObject.TripID,
                    Update = update,
                    PredictiveDepth = predictiveDepth,
                    RegionSize = regionSize
                };

                result.Memory = memUsed;
                result.Time = timeElapsed;
                lock(Results)
                {
                    Results.Add(result);
                }
            }
            objectsBar.Tick($"Moving Object {objectIndex} out of {totalObjects}");
        }

        #region fields
        private const long KB = 1;
        public RoadNetwork RoadNetwork { get; }
        public Dictionary<string, MovingObject> MovingObjects { get; }

        readonly List<dynamic> Results = new List<dynamic>();

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
