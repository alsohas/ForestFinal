namespace ForestFinal
{
    internal class Parameters
    {
        #region files
        private static string BaseFolder = "/Experiments/Porto/";
        public static string TripsFile = $"{BaseFolder}trips_filtered.json";
        public static string NodesFile = $"{BaseFolder}nodes.json";
        public static string EdgesFile = $"{BaseFolder}timed_edges.json";
        #endregion

        #region experiments
        public static int MaxPredictiveDepth = 7;
        public static int MinPredictiveDepth = 2;
        public static int PredictiveDepthIncrement = 1;



        public static double MaxRegionSize = 150;
        public static double MinRegionSize = 50;
        public static double RegionSizeIncrement = 50;

        private static string ResultsFolder = "/Experiments/Results/";
        public static string PredictiveAccuracyFile = $"{ResultsFolder}predictive_results.csv";
        public static string PresentAccuracyFile = $"{ResultsFolder}present_results.csv";

        public static object HistoricalAccuracyFile = $"{ResultsFolder}past_results.csv";


        #endregion
    }
}
