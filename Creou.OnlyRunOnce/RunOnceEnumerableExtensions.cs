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
    public interface IRunOnceEnumerable<out T> : IEnumerable<T>
    {
    }

    public static class RunOnceEnumerableExtensions
    {
        public static IRunOnceEnumerable<T> OnlyRunOnce<T>(this IEnumerable<T> source)
        {
            return new RunOnceEnumerable<T>(source);
        }

        public static IRunOnceEnumerable<T> OnlyRunOnceConcurrentSafe<T>(this IEnumerable<T> source)
        {
            return new ConcurrentRunOnceEnumerable<T>(source);
        }

        private enum EnuermableType
        {
            List,
            Collection,
            Enumerable
        }

        private abstract class RunOnceEnumerableBase<T> : IRunOnceEnumerable<T>, ICollection<T>
        {
            protected IEnumerator<T> _enumerator;
            protected IList<T> _dataList;
            private ICollection<T> _dataCollection = new List<T>();

            private EnuermableType _type;
            protected volatile bool _gotAllData = false;

            protected RunOnceEnumerableBase(IEnumerable<T> enumerable)
            {
                if (enumerable is IList<T>)
                {
                    _type = EnuermableType.List;
                    _dataList = (IList<T>)enumerable;
                }
                else if (enumerable is ICollection<T>)
                {
                    _type = EnuermableType.Collection;
                    _dataCollection = (ICollection<T>)enumerable;
                }
                else
                {
                    _type = EnuermableType.Enumerable;
                    _enumerator = enumerable.GetEnumerator();
                    _dataList = new List<T>();
                }
            }

            protected abstract IEnumerator<T> GetRunOnceEnumerator();

            public IEnumerator<T> GetEnumerator()
            {
                if (_type == EnuermableType.List || _gotAllData)
                {
                    return _dataList.GetEnumerator();
                }
                else if (_type == EnuermableType.Collection)
                {
                    return _dataCollection.GetEnumerator();
                }
                else
                {
                    return GetRunOnceEnumerator();
                }
            }


            int ICollection<T>.Count
            {
                get
                {
                    if (_type == EnuermableType.List || _gotAllData)
                    {
                        return _dataList.Count;
                    }
                    else if (_type == EnuermableType.Collection)
                    {
                        return _dataCollection.Count;
                    }
                    else
                    {
                        int num = 0;
                        using (IEnumerator<T> enumerator = this.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                num++;
                            }
                        }
                        return num;
                    }
                }
            }

            bool ICollection<T>.IsReadOnly => true;

            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            {
                if (_type == EnuermableType.List || _gotAllData)
                {
                    _dataList.CopyTo(array, arrayIndex);
                }
                else if (_type == EnuermableType.Collection)
                {
                    _dataCollection.CopyTo(array, arrayIndex);
                }
                else
                {
                    var _items = new T[0];
                    int index = 0;
                    using (IEnumerator<T> enumerator = this.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (index >= arrayIndex)
                            {
                                array[index - arrayIndex] = (enumerator.Current);
                            }
                            index++;
                        }
                    }
                }
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            void ICollection<T>.Add(T item)
            {
                throw new NotSupportedException();
            }

            void ICollection<T>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<T>.Contains(T item)
            {
                throw new NotSupportedException();
            }

            bool ICollection<T>.Remove(T item)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class RunOnceEnumerable<T> : RunOnceEnumerableBase<T>
        {
           private int _indexedUpTo;

            public RunOnceEnumerable(IEnumerable<T> enumerable)
                : base(enumerable)
            {
            }

            protected override IEnumerator<T> GetRunOnceEnumerator()
            {
                int currentIndex = 0;
                bool isMoreData = true;
                do
                {
                    if (currentIndex < _indexedUpTo)
                    {
                        yield return _dataList[currentIndex];
                    }
                    else
                    {
                        if (isMoreData = _enumerator.MoveNext())
                        {
                            _indexedUpTo++;
                            _dataList.Add(_enumerator.Current);
                            yield return _enumerator.Current;
                        }
                        else
                        {
                            _gotAllData = true;
                            _enumerator.Dispose();
                            _enumerator = null;

                        }
                    }
                    currentIndex++;
                } while (isMoreData);
            }
        }

        private sealed class ConcurrentRunOnceEnumerable<T> : RunOnceEnumerableBase<T>
        {
            private volatile int _indexedUpTo = 0;

            public ConcurrentRunOnceEnumerable(IEnumerable<T> enumerable) : base(enumerable)
            {
            }

            protected override IEnumerator<T> GetRunOnceEnumerator()
            {
                int currentIndex = 0;
                bool isMoreData = true;
                do
                {
                    if (currentIndex < _indexedUpTo)
                    {
                        yield return _dataList[currentIndex];
                    }
                    else if (_gotAllData)
                    {
                        isMoreData = false;
                    }
                    else
                    {
                        T returnData = default(T);
                        bool gotData = false;
                        lock (_dataList)
                        {
                            // Because we don't lock on the other side if this if block, we have to check again here inside this lock that we haven't already got this data, or that all the data has been got already.
                            if (currentIndex < _indexedUpTo)
                            {
                                returnData = _dataList[currentIndex];
                                gotData = true;
                            }
                            else if (_gotAllData)
                            {
                                isMoreData = false;
                            }
                            else
                            {
                                if (isMoreData = _enumerator?.MoveNext() ?? false)
                                {
                                    _dataList.Add(_enumerator.Current);
                                    _indexedUpTo++;
                                    returnData = _enumerator.Current;
                                    gotData = true;
                                }
                                else
                                {
                                    _gotAllData = true;
                                    _enumerator.Dispose();
                                    _enumerator = null;
                                }
                            }
                        }

                        if (gotData)
                        {
                            yield return returnData;
                        }
                    }

                    currentIndex++;
                } while (isMoreData);
            }
        }
    }
}
