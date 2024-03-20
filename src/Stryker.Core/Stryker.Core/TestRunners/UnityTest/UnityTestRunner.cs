using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Stryker.Core.Initialisation;
using Stryker.Core.Logging;
using Stryker.Core.Mutants;
using Stryker.Core.Options;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.DataCollector;
using System.Diagnostics;
using System.Reflection;

namespace Stryker.Core.TestRunners.UnityTest
{
    public sealed class UnityTestRunner : ITestRunner
    {
        private Process _testRun;

        public UnityTestRunner(StrykerOptions options, IFileSystem fileSystem = null)
        {
            _testRun = new Process();
        }

        public IEnumerable<CoverageRunResult> CaptureCoverage(IProjectAndTests project) => throw new NotImplementedException();
        public bool DiscoverTests(string assembly)
        {
            var testDLL = Assembly.LoadFile(assembly);
            foreach (var type in testDLL.GetTypes())
            {
                foreach (var method in type.GetMethods())
                {
                    if (method.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), true).Length > 0) return true;
                }
            }

            return false;
        }
        public void Dispose() => throw new NotImplementedException();
        public TestSet GetTests(IProjectAndTests project) => throw new NotImplementedException();
        public TestRunResult InitialTest(IProjectAndTests project) => throw new NotImplementedException();
        public TestRunResult TestMultipleMutants(IProjectAndTests project, ITimeoutValueCalculator timeoutCalc, IReadOnlyList<Mutant> mutants, TestUpdateHandler update) => throw new NotImplementedException();
    }
}
