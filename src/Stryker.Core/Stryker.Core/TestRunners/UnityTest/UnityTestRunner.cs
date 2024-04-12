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
using System.IO;
using System.Xml;
using Stryker.Core.TestRunners.VsTest;

namespace Stryker.Core.TestRunners.UnityTest
{
    public sealed class UnityTestRunner : ITestRunner
    {
        private StrykerOptions _options;
        private TestSet _testSet;
        private List<Guid> _testGuids;
        private List<TestProject> _testProjects;
        private readonly ILogger _logger;
        private string _unityPath;

        public UnityTestRunner(StrykerOptions options, IEnumerable<SourceProjectInfo> projects)
        {
            _options = options;
            _testSet = new TestSet();
            _testGuids = new List<Guid>();
            _testProjects = new List<TestProject>();
            _logger = ApplicationLogging.LoggerFactory.CreateLogger<UnityTestRunner>();
            _unityPath = options.UnityVersion;

            foreach (var info in projects)
            {
                var testProjects = info.TestProjectsInfo.TestProjects;
                foreach (var testProject in testProjects)
                {
                    _testProjects.Add(testProject);
                }
            }
        }

        public IEnumerable<CoverageRunResult> CaptureCoverage(IProjectAndTests project)
        {


            throw new NotImplementedException();
        }

        public bool DiscoverTests(string assembly)
        {
            var testDLL = Assembly.LoadFrom(assembly);
            foreach (var type in testDLL.GetTypes())
            {
                foreach (var method in type.GetMethods())
                {
                    if (method.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), true).Length > 0
                        || method.GetCustomAttributes(typeof(UnityEngine.TestTools.UnityTestAttribute), true).Length > 0)
                    {
                        var newTestGuid = Guid.NewGuid();
                        var testDesc = new TestDescription(newTestGuid, method.Name, GetTestFilePath(method));    
                        _testSet.RegisterTest(testDesc);
                        _testGuids.Add(newTestGuid);
                    }
                }
            }

            return false;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public TestSet GetTests(IProjectAndTests project) => _testSet;

        public TestRunResult InitialTest(IProjectAndTests project) => RunUnityTests(project, Path.Combine(_options.OutputPath, "initial_test.xml"));

        public TestRunResult TestMultipleMutants(IProjectAndTests project, ITimeoutValueCalculator timeoutCalc, IReadOnlyList<Mutant> mutants, TestUpdateHandler update)
        {
            var mutant = mutants.Single();
            Environment.SetEnvironmentVariable("ActiveMutation", mutant.Id.ToString());

            var testResults = RunUnityTests(project, Path.Combine(_options.OutputPath, string.Concat(mutant.Id.ToString(), ".xml")), additionalArgs:"-disable-assembly-updater");
            update?.Invoke(mutants, testResults.FailingTests, testResults.ExecutedTests, testResults.TimedOutTests);

            mutant.AssessingTests = testResults.ExecutedTests;
            mutant.KillingTests = testResults.FailingTests;
            if (mutant.KillingTests.Count > 0)
                mutant.ResultStatus = MutantStatus.Killed;
            else
                mutant.ResultStatus = MutantStatus.Survived;

            return testResults;
        }

        /// <summary>
        /// Finds the source file of a method appearing in an assembly. This relies on the fact that the class name
        /// is the same as the name of the source file, otherwise it would not work.
        /// </summary>
        /// <param name="testMethod"></param>
        /// <returns>path to the source file where the method is defined</returns>
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

        private TestRunResult RunUnityTests(IProjectAndTests project, string resultPath, string additionalArgs = null)
        {
            _logger.LogDebug("Running Unity tests...");

            string unityEXE = string.Concat("\"", string.Concat(_unityPath, "\\Unity.exe\""));
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = unityEXE;
            processStartInfo.Arguments = string.Concat("-runTests -batchmode -projectPath . -testPlatform EditMode -testResults ", resultPath);

            var testAssemblies = project.GetTestAssemblies();
            var assemblyNames = new List<string>();
            foreach (var testAssembly in testAssemblies)
                assemblyNames.Add(Path.GetFileNameWithoutExtension(testAssembly));
            var assemblyNamesArgument = "\"" + string.Join(";", assemblyNames) + "\"";
            processStartInfo.Arguments = string.Concat(processStartInfo.Arguments, " -assemblyNames ", assemblyNamesArgument);

            if (additionalArgs != null) processStartInfo.Arguments.Concat(" " + additionalArgs);

            processStartInfo.EnvironmentVariables["ActiveMutation"] = Environment.GetEnvironmentVariable("ActiveMutation");

            var testProcess = Process.Start(processStartInfo);
            testProcess.WaitForExit();

            int exitCode = testProcess.ExitCode;
            _logger.LogDebug("Process finished with exit code {1}.", exitCode);

            TestRunResult result;

            if (exitCode == 0 || exitCode == 2)
            {
                var resultDoc = new XmlDocument();
                resultDoc.Load(resultPath);
                var testCaseNodes = resultDoc.GetElementsByTagName("test-case");
                var root = resultDoc.SelectSingleNode("/test-run");
                var timeSpan = new TimeSpan(0, 0, 0, 0, (int)(float.Parse(root.Attributes["duration"].Value) * 1000));

                var executedTests = new List<Guid>();
                var failingTests = new List<Guid>();
                
                foreach (XmlNode testCaseNode in testCaseNodes)
                {
                    string name = testCaseNode.Attributes["name"].Value;
                    string testResult = testCaseNode.Attributes["result"].Value;
                    var testGuid = GetTestGuid(name);

                    if (testResult != "Passed")
                        failingTests.Add(testGuid);
                    executedTests.Add(testGuid);
                }

                result = new TestRunResult(new List<VsTestDescription>(), new TestGuidsList(executedTests), new TestGuidsList(failingTests),
                    TestGuidsList.NoTest(), null, null, timeSpan);
            }
            else result = new TestRunResult(false);

            return result;
        }

        private Guid GetTestGuid(string testName)
        {
            var testGuid = Guid.Empty;

            foreach (var testCase in _testSet.Extract(_testGuids))
            {
                if (testCase.Name.Equals(testName))
                    testGuid = testCase.Id;
            }

            return testGuid;
        }
    }
}
