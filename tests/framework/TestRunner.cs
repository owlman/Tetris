using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace TetrisTests
{
    /// <summary>
    /// Discovers [Test] methods in the calling assembly, runs them and prints a
    /// report. Returns the number of failures, which becomes the process exit
    /// code.
    ///
    /// Command line:
    ///   --filter SUBSTRING   only run tests whose "Class.Method" contains it
    ///   --skip-ui            skip everything in the UI category (needs a desktop)
    ///   --list               list the test names and exit
    /// </summary>
    public static class TestRunner
    {
        private const string UiCategory = "UI";

        private class Case
        {
            public string Name;
            public Type Type;
            public MethodInfo Method;
            public object[] Arguments;
        }

        public static int Run(string[] args)
        {
            string filter = null;
            bool skipUi = false;
            bool listOnly = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--filter" && i + 1 < args.Length)
                    filter = args[++i];
                else if (args[i] == "--skip-ui")
                    skipUi = true;
                else if (args[i] == "--list")
                    listOnly = true;
            }

            List<Case> cases = Discover(filter);

            if (listOnly)
            {
                foreach (Case c in cases)
                    Console.WriteLine(FullName(c));
                Console.WriteLine();
                Console.WriteLine(cases.Count + " test(s)");
                return 0;
            }

            int passed = 0;
            int failed = 0;
            int skipped = 0;
            Stopwatch total = Stopwatch.StartNew();

            foreach (Case c in cases)
            {
                string name = FullName(c);

                if (NeedsUi(c) && skipUi)
                {
                    skipped++;
                    continue;
                }

                object instance = Activator.CreateInstance(c.Type);
                Stopwatch one = Stopwatch.StartNew();
                try
                {
                    c.Method.Invoke(instance, c.Arguments);
                    one.Stop();
                    passed++;
                    Console.WriteLine("[PASS] " + name + "  " + one.ElapsedMilliseconds + "ms");
                }
                catch (TargetInvocationException wrapped)
                {
                    one.Stop();
                    failed++;
                    Exception inner = wrapped.InnerException ?? wrapped;
                    Console.WriteLine("[FAIL] " + name + "  " + one.ElapsedMilliseconds + "ms");
                    if (inner is AssertException)
                        Console.WriteLine("       " + inner.Message);
                    else
                        Console.WriteLine("       " + inner.GetType().Name + ": " + inner.Message);
                }
                catch (Exception ex)
                {
                    one.Stop();
                    failed++;
                    Console.WriteLine("[FAIL] " + name + "  (could not run)");
                    Console.WriteLine("       " + ex.GetType().Name + ": " + ex.Message);
                }
            }

            total.Stop();

            Console.WriteLine();
            Console.WriteLine("Results: " + passed + " passed, " + failed + " failed, "
                + skipped + " skipped, " + (passed + failed + skipped) + " total"
                + " (" + total.ElapsedMilliseconds + " ms)");

            return failed;
        }

        private static List<Case> Discover(string filter)
        {
            List<Case> cases = new List<Case>();
            Assembly assembly = Assembly.GetExecutingAssembly();

            Type[] types = assembly.GetTypes();
            Array.Sort(types, delegate(Type a, Type b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            foreach (Type type in types)
            {
                if (!type.IsClass || type.IsAbstract)
                    continue;
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                Array.Sort(methods, delegate(MethodInfo a, MethodInfo b)
                {
                    return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                });

                foreach (MethodInfo method in methods)
                {
                    if (method.GetCustomAttributes(typeof(TestAttribute), false).Length == 0)
                        continue;

                    object[] caseAttributes = method.GetCustomAttributes(typeof(TestCaseAttribute), false);
                    if (caseAttributes.Length > 0)
                    {
                        foreach (TestCaseAttribute tc in caseAttributes)
                        {
                            Case c = new Case();
                            c.Type = type;
                            c.Method = method;
                            c.Arguments = tc.Arguments;
                            c.Name = method.Name + "(" + Join(tc.Arguments) + ")";
                            AddIfMatches(cases, c, filter);
                        }
                    }
                    else if (method.GetParameters().Length != 0)
                    {
                        // A parameterised method with no [TestCase]: report it
                        // instead of silently skipping, otherwise it just looks
                        // like the test disappeared.
                        Case bad = new Case();
                        bad.Type = type;
                        bad.Method = method;
                        bad.Arguments = new object[0];
                        bad.Name = method.Name + "  <missing TestCase arguments>";
                        AddIfMatches(cases, bad, filter);
                    }
                    else
                    {
                        Case c = new Case();
                        c.Type = type;
                        c.Method = method;
                        c.Arguments = new object[0];
                        c.Name = method.Name;
                        AddIfMatches(cases, c, filter);
                    }
                }
            }

            return cases;
        }

        private static void AddIfMatches(List<Case> cases, Case c, string filter)
        {
            if (filter != null && FullName(c).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                return;
            cases.Add(c);
        }

        private static bool NeedsUi(Case c)
        {
            foreach (CategoryAttribute a in c.Type.GetCustomAttributes(typeof(CategoryAttribute), false))
                if (a.Name == UiCategory) return true;
            foreach (CategoryAttribute a in c.Method.GetCustomAttributes(typeof(CategoryAttribute), false))
                if (a.Name == UiCategory) return true;
            return false;
        }

        private static string FullName(Case c)
        {
            return c.Type.Name + "." + c.Name;
        }

        private static string Join(object[] values)
        {
            if (values == null || values.Length == 0) return "";
            string s = "";
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) s += ", ";
                s += values[i] == null ? "null" : values[i].ToString();
            }
            return s;
        }
    }
}
