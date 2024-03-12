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

namespace Stryker.Core.TestRunners.UnityTest;
public sealed class UnityTestRunner : ITestRunner
{
    public IEnumerable<CoverageRunResult> CaptureCoverage(IProjectAndTests project) => throw new NotImplementedException();
    public bool DiscoverTests(string assembly) => throw new NotImplementedException();
    public void Dispose() => throw new NotImplementedException();
    public TestSet GetTests(IProjectAndTests project) => throw new NotImplementedException();
    public TestRunResult InitialTest(IProjectAndTests project) => throw new NotImplementedException();
    public TestRunResult TestMultipleMutants(IProjectAndTests project, ITimeoutValueCalculator timeoutCalc, IReadOnlyList<Mutant> mutants, TestUpdateHandler update) => throw new NotImplementedException();
}
