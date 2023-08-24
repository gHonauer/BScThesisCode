using System.Diagnostics;

namespace SumSat
{
    internal static class HelperFunctions
    {
        /// <summary>
        /// Ensures the given <see cref="bool"/> is true, and throws an exception otherwise.
        /// </summary>
        /// <param name="t">The condition that should be true.</param>
        /// <param name="msg">The optional error message to show upon failure.</param>
        /// <exception cref="Exception">Thrown if the assertion fails.</exception>
        [DebuggerHidden]
        internal static void Assert(bool t, string? msg = null)
        {
            if (!t)
                if (msg != null)
                    throw new Exception("Assertion failed: " + msg);
                else
                    throw new Exception("Assertion failed");
        }
    }
}
