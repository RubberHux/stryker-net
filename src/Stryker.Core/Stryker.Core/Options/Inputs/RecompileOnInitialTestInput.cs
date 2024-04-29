using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stryker.Core.Options.Inputs
{
    public class RecompileOnInitialTestInput : Input<bool>
    {
        public override bool Default => true;

        protected override string Description => "Instruct Stryker to recompile project when running tests (Unity project only)";

        public bool Validate() => SuppliedInput == true;
    }
}
