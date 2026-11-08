using System;

namespace TetrisTests
{
    /// <summary>
    /// Marks a public parameterless method as a test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TestAttribute : Attribute
    {
        /// <summary>Optional display name; defaults to the method name.</summary>
        public string Name { get; set; }

        public TestAttribute()
        {
        }

        public TestAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Supplies one set of arguments for a parameterised test. Repeat the
    /// attribute to run the same method several times.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class TestCaseAttribute : Attribute
    {
        public object[] Arguments { get; private set; }

        public TestCaseAttribute(params object[] arguments)
        {
            Arguments = arguments == null ? new object[0] : arguments;
        }
    }

    /// <summary>
    /// Groups tests; the runner can skip a whole group (see --skip-ui), which
    /// matters for the drawing tests because they need a desktop session.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CategoryAttribute : Attribute
    {
        public string Name { get; private set; }

        public CategoryAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>Thrown by <see cref="Assert"/> when an expectation fails.</summary>
    public class AssertException : Exception
    {
        public AssertException(string message) : base(message)
        {
        }
    }
}
