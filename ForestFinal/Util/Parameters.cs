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
        public static int PredictiveDepth = 3;
        public static double RegionSize = 100;
        #endregion
    }
}
