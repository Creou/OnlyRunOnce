namespace System.Collections.Generic
{
    public static class RunOnceEnumerableExtensions
    {
        public static IRunOnceEnumerable<T> OnlyRunOnce<T>(this IEnumerable<T> source)
        {
            return new RunOnceEnumerable<T>(source);
        }

        public interface IRunOnceEnumerable<T> : IEnumerable<T>
        {
        }

        private class RunOnceEnumerable<T> : IRunOnceEnumerable<T>
        {
            private IEnumerable<T> enumerable;
            private IEnumerator<T> enumerator;
            int indexedUpTo;
            private List<T> data = new List<T>();

            public RunOnceEnumerable(IEnumerable<T> enumerable)
            {
                this.enumerable = enumerable;
                this.enumerator = enumerable.GetEnumerator();
            }

            public IEnumerator<T> GetEnumerator()
            {
                int currentIndex = 0;
                bool isMoreData = true;
                do
                {
                    if (currentIndex < indexedUpTo)
                    {
                        yield return data[currentIndex];
                    }
                    else
                    {
                        if (isMoreData = enumerator.MoveNext())
                        {
                            indexedUpTo++;
                            data.Add(enumerator.Current);
                            yield return enumerator.Current;
                        }
                    }
                    currentIndex++;
                } while (isMoreData);

            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
