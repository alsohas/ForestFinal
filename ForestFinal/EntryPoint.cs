using ForestFinal.Experiments;
using ForestFinal.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ForestFinal
{
    internal class EntryPoint
    {
        public static RoadNetwork RoadNetwork;
        public static Dictionary<string, MovingObject> MovingObjects;

        private static void Main()
        {
            Console.WriteLine("Started road network construction");
            Stopwatch timer = Stopwatch.StartNew();

            RoadNetwork = new RoadNetwork();
            MovingObjects = new Dictionary<string, MovingObject>();
            Init.Initialize(RoadNetwork, MovingObjects);
            timer.Stop();
            long elapsedMs = timer.ElapsedMilliseconds;
            Console.WriteLine($"Time took to construct road network graph and read moving objects: {elapsedMs} milliseconds");
            StartExperiments();
        }

        private static void StartExperiments()
        {
            AccuracyEvals accEvals = new AccuracyEvals(RoadNetwork, MovingObjects);
            accEvals.Start();
        }
    }
}
