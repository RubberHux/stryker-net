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
using Stryker.Core.ProjectComponents.SourceProjects;

namespace Stryker.Core.TestRunners.UnityTest
{
    public sealed class UnityTestRunner : ITestRunner
    {
        private TestSet _testSet;
        private List<TestProject> _testProjects;

        public UnityTestRunner(StrykerOptions options, IEnumerable<SourceProjectInfo> projects)
        {
            _testSet = new TestSet();
            _testProjects = new List<TestProject>();

            foreach (var info in projects)
            {
                var testProjects = info.TestProjectsInfo.TestProjects;
                foreach (var testProject in testProjects)
                {
                    _testProjects.Add(testProject);
                }
            }
        }

        public IEnumerable<CoverageRunResult> CaptureCoverage(IProjectAndTests project) => throw new NotImplementedException();

        public bool DiscoverTests(string assembly)
        {
            var testDLL = Assembly.LoadFrom(assembly);
            foreach (var type in testDLL.GetTypes())
            {
                foreach (var method in type.GetMethods())
                {
                    if (method.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), true).Length > 0)
                    {
                        // NOTE: Currently, this does not take the Unity test tag into account, meaning it
                        // can only identify NUnit test cases. This will need to be improved later
                        var testDesc = new TestDescription(Guid.NewGuid(), method.Name, GetTestFilePath(method));    
                        _testSet.RegisterTest(testDesc);
                    }
                }
            }

            return false;
        }

        public void Dispose() => throw new NotImplementedException();

        public TestSet GetTests(IProjectAndTests project) => _testSet;

        public TestRunResult InitialTest(IProjectAndTests project) => throw new NotImplementedException();

        public TestRunResult TestMultipleMutants(IProjectAndTests project, ITimeoutValueCalculator timeoutCalc, IReadOnlyList<Mutant> mutants, TestUpdateHandler update) => throw new NotImplementedException();

        private string GetTestFilePath(MethodInfo testMethod)
        {
            string result = string.Empty;

            foreach (var testProject in _testProjects)
            {
                var testFiles = testProject.TestFiles;
                foreach (var testFile in testFiles)
                {
                    if (testFile.FilePath.Contains(testMethod.DeclaringType.Name))
                    {
                        result = testFile.FilePath;
                        break;
                    }
                }
                if (result != string.Empty) break;
            }

            return result;
        }
    }
}
