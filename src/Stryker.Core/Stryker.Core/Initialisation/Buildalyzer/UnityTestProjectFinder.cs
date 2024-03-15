using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using Buildalyzer;
using Microsoft.CodeAnalysis.CSharp;

namespace Stryker.Core.Initialisation.Buildalyzer
{
    public class UnityTestProjectFinder
    {
        /// <summary>
        /// Determines whether a project in the solution is a Unity test project or not.
        /// </summary>
        /// <param name="analyzerResult">The project to verify</param>
        /// <returns></returns>
        public static bool IsUnityTestProject(IAnalyzerResult project)
        {
            bool sourceFileHasTests = false;
            foreach (var sourceFile in project.SourceFiles)
            {
                var sr = new StreamReader(sourceFile);
                string currentLine;
                while ((currentLine = sr.ReadLine()) != null)
                {
                    if (currentLine.Contains("[Test]") || currentLine.Contains("[UnityTest]"))
                    {
                        sourceFileHasTests = true;
                        break;
                    }
                }
                if (sourceFileHasTests)
                    break;
            }
            return project.References.Any(r => r.Contains("UnityEngine.TestRunner")) && sourceFileHasTests;
        }
    }
}

