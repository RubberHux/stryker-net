using UnityTestProject.Scripts;
using NUnit.Framework;

namespace UnityTestProject.EditModeTests
{
    public class ExtraTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void IncrementorTest2()
        {
            // Use the Assert class to test conditions
            Assert.AreNotEqual(Incrementor.Increment(-1), 0);
        }
    }
}
