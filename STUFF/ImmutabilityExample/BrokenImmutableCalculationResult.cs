using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STUFF.ImmutabilityExample
{
    public class BrokenImmutableCalculationResult : ICalculationResult
    {
        public int Total { get; private set; }
        public BrokenImmutableCalculationResult(int total)
        {
            Total = total;

        }
    }
}
