using NUnit.Framework;

namespace Enklu.Orchid.Chakra.Tests
{
    /// <summary>
    /// NUnit assertion utility
    /// </summary>
    public static class Asserter
    {
        /// <summary>
        /// Determines if the two arrays contents are equivalent.
        /// </summary>
        public static void AreEqual<T>(T[] first, T[] second)
        {
            Assert.IsTrue(first.Length == second.Length);
            for (int i = 0; i < first.Length; ++i)
            {
                Assert.AreEqual(first[i], second[i]);
            }
        }
    }
}