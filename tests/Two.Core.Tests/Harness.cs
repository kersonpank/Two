using System;
using System.Collections.Generic;

namespace Two.Core.Tests
{
    /// <summary>
    /// Micro-harness de testes sem dependências externas (roda offline em qualquer CI).
    /// Na Unity, os mesmos casos podem ser portados para o Test Framework depois.
    /// </summary>
    public static class Harness
    {
        static readonly List<(string Name, Action Body)> Tests = new List<(string, Action)>();
        static string _current;

        public static void Test(string name, Action body) => Tests.Add((name, body));

        public static int RunAll()
        {
            int passed = 0, failed = 0;
            foreach (var (name, body) in Tests)
            {
                _current = name;
                try
                {
                    body();
                    Console.WriteLine($"  PASS  {name}");
                    passed++;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"  FAIL  {name}");
                    Console.WriteLine($"        {e.Message}");
                    failed++;
                }
            }
            Console.WriteLine();
            Console.WriteLine($"{passed} passed, {failed} failed, {Tests.Count} total");
            return failed == 0 ? 0 : 1;
        }

        public static void IsTrue(bool condition, string what)
        {
            if (!condition) throw new Exception($"esperava TRUE: {what}");
        }

        public static void Approx(float actual, float expected, float tolerance, string what)
        {
            if (Math.Abs(actual - expected) > tolerance)
                throw new Exception($"{what}: esperava {expected} ± {tolerance}, obteve {actual}");
        }

        public static void InRange(float actual, float min, float max, string what)
        {
            if (actual < min || actual > max)
                throw new Exception($"{what}: esperava [{min}, {max}], obteve {actual}");
        }
    }

    /// <summary>Mundo de teste: chão plano opcional + lista de sólidos.</summary>
    public sealed class TestWorld : ICollisionWorld
    {
        public bool FloorEnabled = true;
        public float FloorY = 0f;
        public readonly List<AABB> Solids = new List<AABB>();

        public bool Collides(in AABB box)
        {
            if (FloorEnabled && box.Bottom < FloorY) return true;
            foreach (var s in Solids)
                if (box.Overlaps(s)) return true;
            return false;
        }
    }
}
