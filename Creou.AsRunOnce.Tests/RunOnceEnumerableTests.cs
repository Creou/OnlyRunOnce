using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace Creou.AsRunOnce.Tests
{
    [TestClass]
    public class RunOnceEnumerableTests
    {
        [TestMethod]
        public void ForEach_IsRunOnce()
        {
            const int numberInRange = 100;
            List<int> data = Enumerable.Range(0, numberInRange).ToList();

            Dictionary<int, int> runCount = new Dictionary<int, int>();

            var asStrings = data.Select(s =>
            {
                if (runCount.ContainsKey(s))
                {
                    runCount[s]++;
                }
                else
                {
                    runCount[s] = 1;
                }

                return s.ToString();
            })
            .OnlyRunOnce();

            int count = 0;
            foreach (var item in asStrings)
            {
                count++;
            }
            int count2 = 0;
            foreach (var item in asStrings)
            {
                count2++;
            }

            Assert.AreEqual(numberInRange, count);
            Assert.AreEqual(numberInRange, count);

            Assert.IsTrue(runCount.All(r => r.Value == 1), "All run counts must be 1");
            Assert.IsTrue(runCount.Count == numberInRange, $"Must be {numberInRange} run counts.");
        }

        [TestMethod]
        public void ParallelForEach_IsRunOnce()
        {
            const int numberInRange = 100;
            List<int> data = Enumerable.Range(0, numberInRange).ToList();

            Dictionary<int, int> runCount = new Dictionary<int, int>();

            var asStrings = data.Select(s =>
            {
                if (runCount.ContainsKey(s))
                {
                    runCount[s]++;
                }
                else
                {
                    runCount[s] = 1;
                }

                return s.ToString();
            })
            .OnlyRunOnce();

            ConcurrentDictionary<int, string> resultData = new ConcurrentDictionary<int, string>();
            Parallel.ForEach(asStrings, (s) =>
            {
                resultData.TryAdd(int.Parse(s), s);
            });

            Assert.IsTrue(resultData.Count == numberInRange);
            foreach (var item in data)
            {
                Assert.IsTrue(resultData.ContainsKey(item));
            }
            Assert.IsTrue(runCount.All(r => r.Value == 1), "All run counts must be 1");
            Assert.IsTrue(runCount.Count == numberInRange, $"Must be {numberInRange} run counts.");
        }

        [TestMethod]
        public void ConsecutiveJoin_IsRunOnce()
        {
            const int numberInRange = 100;
            List<int> data = Enumerable.Range(0, numberInRange).ToList();
            string expectedResult = string.Join(",", data);

            Dictionary<int, int> runCount = new Dictionary<int, int>();

            var asStrings = data.Select(s =>
            {
                if (runCount.ContainsKey(s))
                {
                    runCount[s]++;
                }
                else
                {
                    runCount[s] = 1;
                }

                return s.ToString();
            })
            .OnlyRunOnce();

            var result1 = string.Join(",", asStrings);
            var result2 = string.Join(",", asStrings);

            Assert.AreEqual(result1, expectedResult, "Result1 string must be correct");
            Assert.AreEqual(result2, expectedResult, "Result2 string must be correct");

            Assert.IsTrue(runCount.All(r => r.Value == 1), "All run counts must be 1");
            Assert.IsTrue(runCount.Count == numberInRange, $"Must be {numberInRange} run counts.");
        }

        [TestMethod]
        public void TakeAndSkip_IsRunOnce()
        {
            const int numberInRange = 100;
            List<int> data = Enumerable.Range(0, numberInRange).ToList();
            string expectedResult = string.Join(",", data);

            Dictionary<int, int> runCount = new Dictionary<int, int>();

            var asStrings = data.Select(s =>
            {
                if (runCount.ContainsKey(s))
                {
                    runCount[s]++;
                }
                else
                {
                    runCount[s] = 1;
                }

                return s.ToString();
            })
            .OnlyRunOnce();

            List<string> firstFive = asStrings.Take(5).ToList();
            Assert.AreEqual(5, firstFive.Count);
            Assert.IsTrue(runCount.All(r => r.Value == 1), "All run counts must be 1");
            Assert.IsTrue(runCount.Count == 5, $"Must be {5} run counts.");

            List<string> secondTen = asStrings.Skip(10).Take(10).ToList();
            Assert.AreEqual(10, secondTen.Count);
            Assert.IsTrue(runCount.All(r => r.Value == 1), "All run counts must be 1");
            Assert.IsTrue(runCount.Count == 20, $"Must be {20} run counts.");

            List<string> firstTwenty = asStrings.Take(20).ToList();
            Assert.AreEqual(20, firstTwenty.Count);
            Assert.IsTrue(runCount.All(r => r.Value == 1), "All run counts must be 1");
            Assert.IsTrue(runCount.Count == 20, $"Must be {20} run counts.");

            List<string> all = asStrings.ToList();
            Assert.AreEqual(numberInRange, all.Count);
            Assert.IsTrue(runCount.All(r => r.Value == 1), "All run counts must be 1");
            Assert.IsTrue(runCount.Count == numberInRange, $"Must be {numberInRange} run counts.");
        }
    }
}