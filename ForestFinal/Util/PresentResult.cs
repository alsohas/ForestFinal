using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForestFinal.Util
{
    class PresentResult
    {
        public string TripID { get; set; }
        public int Update { get; set; }
        public double Accuracy { get; set; }
        public double RegionSize { get; set; }
        public PresentResult() { }
        public override string ToString()
        {
            return $"Trip: {TripID}, Update: {Update}, Accuracy: {Accuracy}";
        }
    }
}
