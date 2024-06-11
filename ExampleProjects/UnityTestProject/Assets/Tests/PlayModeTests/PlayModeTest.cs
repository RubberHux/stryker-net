using NUnit.Framework;
using UnityTestProject.Scripts;

namespace UnityTestProject.PlayModeTests
{
    public class PlayModeTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void ExamplePlayModeTest()
        {
            Assert.AreEqual(Incrementor.Increment(0), 1);
        }
    }
}
