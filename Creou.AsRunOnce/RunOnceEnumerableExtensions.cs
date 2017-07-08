/*
 * MIT License
 * 
 * Copyright (c) 2017 Creou Limited
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 */

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
