using System;
using System.Collections;

namespace TetrisTests
{
    /// <summary>
    /// Minimal assertion helpers. Deliberately small: the point of this harness
    /// is to get real unit tests running on a machine that only has a C# 5
    /// compiler, not to reimplement a full assertion library.
    ///
    /// Every failure throws <see cref="AssertException"/>, which the runner turns
    /// into a [FAIL] line.
    /// </summary>
    public static class Assert
    {
        public static void Fail(string message)
        {
            throw new AssertException(message);
        }

        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
                throw new AssertException("Expected true, but was false. " + message);
        }

        public static void IsFalse(bool condition, string message)
        {
            if (condition)
                throw new AssertException("Expected false, but was true. " + message);
        }

        public static void AreEqual(object expected, object actual, string message)
        {
            if (!Equal(expected, actual))
                throw new AssertException(
                    "Expected: " + Show(expected) + "  But was: " + Show(actual) + "  " + message);
        }

        public static void AreNotEqual(object notExpected, object actual, string message)
        {
            if (Equal(notExpected, actual))
                throw new AssertException(
                    "Expected anything but: " + Show(notExpected) + "  " + message);
        }

        public static void IsNull(object value, string message)
        {
            if (value != null)
                throw new AssertException("Expected null, but was: " + Show(value) + "  " + message);
        }

        public static void IsNotNull(object value, string message)
        {
            if (value == null)
                throw new AssertException("Expected not null. " + message);
        }

        public static void IsSame(object expected, object actual, string message)
        {
            if (!object.ReferenceEquals(expected, actual))
                throw new AssertException("Expected the same instance. " + message);
        }

        public static void IsNotSame(object unexpected, object actual, string message)
        {
            if (object.ReferenceEquals(unexpected, actual))
                throw new AssertException("Expected a different instance. " + message);
        }

        public static void Count(int expected, ICollection collection, string message)
        {
            if (collection == null)
                throw new AssertException("Expected a collection with " + expected + " items, got null. " + message);
            if (collection.Count != expected)
                throw new AssertException(
                    "Expected " + expected + " item(s), got " + collection.Count + ". " + message);
        }

        /// <summary>
        /// Asserts that the action throws T. Returns the exception so the test can
        /// check its message or properties.
        /// </summary>
        public static T Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T expected)
            {
                return expected;
            }
            catch (Exception other)
            {
                throw new AssertException(
                    "Expected " + typeof(T).Name + " but got " + other.GetType().Name
                    + ": " + other.Message + "  " + message);
            }

            throw new AssertException("Expected " + typeof(T).Name + " but nothing was thrown. " + message);
        }

        /// <summary>
        /// Asserts that the action throws nothing. Used for the "must not blow up
        /// on an out of range request" cases.
        /// </summary>
        public static void DoesNotThrow(Action action, string message)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new AssertException(
                    "Expected no exception but got " + ex.GetType().Name + ": " + ex.Message
                    + "  " + message);
            }
        }

        private static bool Equal(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // Numbers are compared by value across types: Assert.AreEqual(128,
            // color.R) is the natural way to write an assertion, and colour
            // channels are bytes while the literal is an int.
            if (IsNumber(a) && IsNumber(b))
                return Convert.ToDecimal(a) == Convert.ToDecimal(b);

            if (a is IComparable && a.GetType() == b.GetType())
                return ((IComparable)a).CompareTo(b) == 0;

            return a.Equals(b);
        }

        private static bool IsNumber(object value)
        {
            return value is byte || value is sbyte
                || value is short || value is ushort
                || value is int || value is uint
                || value is long || value is ulong
                || value is float || value is double
                || value is decimal;
        }

        private static string Show(object value)
        {
            if (value == null) return "null";
            if (value is string) return "\"" + value + "\"";
            return value.ToString() + " (" + value.GetType().Name + ")";
        }
    }
}
