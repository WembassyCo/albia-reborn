using NUnit.Framework;

namespace AlbiaReborn.Tests.EditMode
{
    /// <summary>
    /// Placeholder test fixture for Edit Mode testing.
    /// This serves as a sanity check that the test framework is properly configured.
    /// </summary>
    public class PlaceholderTest
    {
        [Test]
        public void SanityCheck()
        {
            Assert.Pass("Test framework is properly configured.");
        }

        [Test]
        public void BasicMathWorks()
        {
            Assert.AreEqual(4, 2 + 2, "Basic arithmetic should function correctly.");
        }

        [Test]
        public void UnityEditorIsAvailable()
        {
            // This test verifies the Unity Editor test runner is available
            #if UNITY_EDITOR
                Assert.Pass("Unity Editor is defined.");
            #else
                Assert.Fail("Expected to be running in Unity Editor for Edit Mode tests.");
            #endif
        }
    }
}
