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
using Newtonsoft.Json;

namespace Stryker.Core.TestRunners.UnityTest
{
    public enum UnityTestPlatform { EditMode, PlayMode };

    public sealed class UnityTestRunner : ITestRunner
    {
        private StrykerOptions _options;
        private TestSet _testSet;
        private List<Guid> _testGuids;
        private List<TestProject> _testProjects;
        private readonly ILogger _logger;

        public UnityTestRunner(StrykerOptions options, IEnumerable<SourceProjectInfo> projects)
        {
            _options = options;
            _testSet = new TestSet();
            _testGuids = new List<Guid>();
            _testProjects = new List<TestProject>();
            _logger = ApplicationLogging.LoggerFactory.CreateLogger<UnityTestRunner>();

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
            // only register tests from the assembly if the corresponding test project was supplied as input
            if (IsSuppliedTestAssembly(assembly))
            {
                var testDLL = Assembly.LoadFrom(assembly);

                // filter out types that result in errors, if there are any
                IEnumerable<Type> types;
                try
                {
                    types = testDLL.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null);
                }

                foreach (var type in types)
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
            }

            return false;
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
                if (result != string.Empty)
                    break;
            }

            return result;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public TestSet GetTests(IProjectAndTests project) => _testSet;

        public TestRunResult InitialTest(IProjectAndTests project)
        {
            var recompileArg = _options.RecompileOnInitialTestInput ? "-disable-assembly-updater" : string.Empty;
            return RunAllUnityTests(project, "initial_test", TimeSpan.MaxValue, additionalArgs: recompileArg);
        }

        public TestRunResult TestMultipleMutants(IProjectAndTests project, ITimeoutValueCalculator timeoutCalc, IReadOnlyList<Mutant> mutants, TestUpdateHandler update)
        {
            var mutant = mutants.Single();
            Environment.SetEnvironmentVariable("ActiveMutation", mutant.Id.ToString());

            var testResults = RunAllUnityTests(project, Path.Combine(_options.OutputPath, mutant.Id.ToString()), TimeSpan.FromMilliseconds(timeoutCalc.DefaultTimeout), additionalArgs:"-disable-assembly-updater");

            mutant.AssessingTests = testResults.ExecutedTests;
            mutant.KillingTests = testResults.FailingTests;
            if (mutant.KillingTests.Count > 0)
                mutant.ResultStatus = MutantStatus.Killed;
            else if (testResults.TimedOutTests.Count > 0)
                mutant.ResultStatus = MutantStatus.Timeout;
            else
                mutant.ResultStatus = MutantStatus.Survived;

            return testResults;
        }

        private TestRunResult RunAllUnityTests(IProjectAndTests project, string resultDir, TimeSpan timeout, string additionalArgs = null)
        {
            var resultPath = Path.Combine(_options.OutputPath, resultDir);

            var editModeResults = RunUnityTests(project, "\"" + resultPath + "\\editmode.xml\"", UnityTestPlatform.EditMode, timeout, additionalArgs);
            var playModeResults = RunUnityTests(project, "\"" + resultPath + "\\playmode.xml\"", UnityTestPlatform.PlayMode, timeout,additionalArgs);

            return CombineTestResults(editModeResults, playModeResults);
            //return editModeResults;
        }

        private TestRunResult CombineTestResults(TestRunResult first, TestRunResult second)
        {
            return new TestRunResult(new List<VsTestDescription>(),
                first.ExecutedTests.Merge(second.ExecutedTests),
                first.FailingTests.Merge(second.FailingTests),
                first.TimedOutTests.Merge(second.TimedOutTests),
                null,
                null,
                TimeSpan.FromMilliseconds(first.Duration.Milliseconds + second.Duration.Milliseconds));
        }

        private TestRunResult RunUnityTests(IProjectAndTests project, string resultPath, UnityTestPlatform testPlatform, TimeSpan timeout, string additionalArgs = null)
        {
            _logger.LogDebug("Running Unity tests...");

            string unityEXE = string.Concat("\"", string.Concat(_options.UnityPath, "\\Unity.exe\""));
            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = unityEXE;
            string platform = Enum.GetName(testPlatform);
            processStartInfo.Arguments = string.Concat("-runTests -batchmode -nographics -projectPath . -testPlatform ", platform, " -testResults ", resultPath);

            var testAssemblies = GetTestAssemblies(project, testPlatform);
            if (testAssemblies.Count() == 0)
            {
                _logger.LogDebug("No test suites for test platform " + platform + ". Returning empty results.");
                return new TestRunResult(new List<VsTestDescription>(), TestGuidsList.NoTest(), TestGuidsList.NoTest(),
                    TestGuidsList.NoTest(), null, null, TimeSpan.Zero);
            }

            var assemblyNamesArgument = "\"" + string.Join(";", testAssemblies) + "\"";
            processStartInfo.Arguments += " -assemblyNames " + assemblyNamesArgument;

            if (additionalArgs != null) processStartInfo.Arguments += " " + additionalArgs;

            processStartInfo.EnvironmentVariables["ActiveMutation"] = Environment.GetEnvironmentVariable("ActiveMutation");

            var testProcess = Process.Start(processStartInfo);
            if (timeout == TimeSpan.MaxValue) testProcess.WaitForExit();
            else testProcess.WaitForExit(timeout);

            int exitCode = -1;
            if (testProcess.HasExited) exitCode = testProcess.ExitCode;
            else testProcess.Kill();
            _logger.LogDebug("Process finished with exit code {1}.", exitCode);

            TestRunResult result;

            if (exitCode == 0 || exitCode == 2)
            {
                var resultDoc = new XmlDocument();
                resultDoc.Load(resultPath.Substring(1, resultPath.Length - 2));
                var testCaseNodes = resultDoc.GetElementsByTagName("test-case");
                var root = resultDoc.SelectSingleNode("/test-run");
                var timeSpan = TimeSpan.FromMilliseconds((int)(float.Parse(root.Attributes["duration"].Value.Replace('.', ',')) * 1000));

                var executedTests = new List<Guid>();
                var failingTests = new List<Guid>();
                var errorTests = new List<Guid>();

                foreach (XmlNode testCaseNode in testCaseNodes)
                {
                    string name = testCaseNode.Attributes["name"].Value;
                    string testResult = testCaseNode.Attributes["result"].Value;
                    var testGuid = GetTestGuid(name);

                    if (testResult != "Passed")
                        if (testCaseNode.Attributes["label"] == null)
                            failingTests.Add(testGuid);
                        else if (testCaseNode.Attributes["label"].Value == "Error")
                            errorTests.Add(testGuid);
                    executedTests.Add(testGuid);
                }

                result = new TestRunResult(new List<VsTestDescription>(), new TestGuidsList(executedTests), new TestGuidsList(failingTests),
                    TestGuidsList.NoTest(), null, null, timeSpan);
            }
            else if (exitCode == -1)
                return TestRunResult.TimedOut(new List<VsTestDescription>(), TestGuidsList.NoTest(), TestGuidsList.NoTest(),
                new TestGuidsList(_testGuids), null, null, TimeSpan.Zero);
            else
                result = new TestRunResult(false);

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

        private IEnumerable<string> GetTestAssemblies(IProjectAndTests project, UnityTestPlatform testPlatform)
        {
            var result = new List<string>();
            var testAssemblies = project.GetTestAssemblies();
            foreach (var testAssembly in testAssemblies)
            {
                if (GetAssemblyTestPlatform(testAssembly).Equals(testPlatform) && IsSuppliedTestAssembly(testAssembly))
                    result.Add(Path.GetFileNameWithoutExtension(testAssembly));
            }
            return result;
        }

        private UnityTestPlatform GetAssemblyTestPlatform(string testAssembly)
        {
            UnityTestPlatform result;

            string assemblyReferencePath = GetAssemblyDefinitionPath(GetTestProject(testAssembly), testAssembly);
            var reader = new StreamReader(assemblyReferencePath);
            var json = reader.ReadToEnd();
            var asmdef = JsonConvert.DeserializeObject<AssemblyDefinition>(json);

            if (asmdef.IncludePlatforms.Contains("Editor"))
                result = UnityTestPlatform.EditMode;
            else
                result = UnityTestPlatform.PlayMode;

            return result;
        }

        private TestProject GetTestProject(string testAssembly)
        {
            string assemblyName = Path.GetFileNameWithoutExtension(testAssembly);
            foreach (var testProject in _testProjects)
            {
                if (Path.GetFileNameWithoutExtension(testProject.AnalyzerResult.ProjectFilePath) == assemblyName)
                    return testProject;
            }

            return null;
        }

        private string GetAssemblyDefinitionPath(TestProject testProject, string testAssembly)
        {
            string assemblyName = Path.GetFileNameWithoutExtension(testAssembly);
            foreach (var item in testProject.AnalyzerResult.Items)
            {
                if (item.Key == "None")
                {
                    foreach (var value in item.Value)
                    {
                        if (value.ItemSpec.Contains(assemblyName) && Path.GetExtension(value.ItemSpec) == ".asmdef")
                            return value.ItemSpec;
                    }
                }
            }

            return null;
        }

        private bool IsSuppliedTestAssembly(string testAssembly)
        {
            return _options.TestProjects.Any(i => Path.GetFileNameWithoutExtension(testAssembly) == Path.GetFileNameWithoutExtension(i));
        }
    }
}
