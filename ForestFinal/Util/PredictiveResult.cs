namespace ForestFinal.Util
{
    internal class PredictiveResult
    {
        public string TripID { get; set; }
        public int Update { get; set; }
        public double Accuracy { get; set; }
        public int PredictiveDepth { get; set; }
        public double RegionSize { get; set; }
        public int NodeCount { get; set; }

        public PredictiveResult() { }

        public override string ToString()
        {
            return $"Trip: {TripID}, Update: {Update}, PredictiveDepth: {PredictiveDepth}, Accuracy: {Accuracy}";
        }
    }
}
