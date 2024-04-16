using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stryker.Core.TestRunners.UnityTest
{
    /// <summary>
    /// Representation of a Unity assembly definition
    /// </summary>
    public class AssemblyDefinition
    {
        public bool AllowUnsafeCode { get; set; }
        public bool AutoReferenced { get; set; }
        public string[] DefineConstraints { get; set; }
        public string[] ExcludePlatforms { get; set; }
        public string[] IncludePlatforms { get; set; }
        public string Name { get; set; }
        public bool NoEngineReferences { get; set; }
        public string[] OptionalUnityReferences { get; set; }
        public bool OverrideReferences { get; set; }
        public string[] PrecompiledReferences { get; set; }
        public string[] References { get; set; }
        public object[] VersionDefines { get; set; }
    }
}
