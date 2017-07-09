using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Generic.RunOnceEnumerableExtensions;

namespace Creou.OnlyRunOnce.Tests
{
    [TestClass]
    public class CovarianceTests
    {
        [TestMethod]
        public void CovariantTest()
        {
            const int numberInRange = 100;
            List<int> data = Enumerable.Range(0, numberInRange).ToList();
            string expectedResult = string.Join(",", data);

            IRunOnceEnumerable<ChildClass> e = data.Select(s => new ChildClass(s)).OnlyRunOnce();

            // This is really a compiler time test.
            // This method will only be callable if IRunOnceEnumerable is covariant.
            // It will produce a compiler error otherwise.
            string actualResult = CovariantAction(e);

            Assert.AreEqual(expectedResult, actualResult);
        }

        private string CovariantAction(IRunOnceEnumerable<ParentClass> data)
        {
            return string.Join(",", data.Select(d => d.StringData));
        }

        private class ParentClass
        {
            public ParentClass(string data)
            {
                this.StringData = data;
            }

            public string StringData { get; private set; }
        }

        private class ChildClass : ParentClass
        {
            public ChildClass(int data)
                : base(data.ToString())
            {
                this.IntData = data;
            }

            public int IntData { get; private set; }
        }
    }
}
