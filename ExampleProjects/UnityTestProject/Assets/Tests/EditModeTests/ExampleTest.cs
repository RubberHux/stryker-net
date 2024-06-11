using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityTestProject.Scripts;

namespace UnityTestProject.EditModeTests
{
    public class ExampleTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void IncrementorTest1()
        {
            // Use the Assert class to test conditions
            int res = Incrementor.Increment(1);
            Assert.AreEqual(res, 2);
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator IncrementorTestUnity()
        {
            // Use the Assert class to test conditions.
            int res = Incrementor.Increment(-2);
            Assert.AreEqual(res, -3);
            // Use yield to skip a frame.
            yield return null;
        }

        [Test]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(-4)]
        public void ParameterizedTest(int input)
        {
            if (input >= 0) Assert.AreEqual(Incrementor.Increment(input), input + 1);
            else Assert.AreEqual(Incrementor.Increment(input), input - 1);
        }
    }
}
