using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForestFinal.Util
{
    class PerformanceResult
    {
        public string TripID { get; set; }
        public int Update { get; set; }
        public int PredictiveDepth { get; set; }
        public double RegionSize { get; set; }
        public double Memory { get; set; }
        public double Time { get; set; }
        public PerformanceResult() { }

        public override string ToString()
        {
            return $"Trip: {TripID}, Update: {Update}, Mem: {Memory}, Time: {Time}";
        }
    }
}
