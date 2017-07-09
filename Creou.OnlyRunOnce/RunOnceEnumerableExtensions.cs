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

using System.Linq;

namespace System.Collections.Generic
{
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

        public interface IRunOnceEnumerable<T> : IEnumerable<T>
        {
            List<T> ToList();
        }

        private sealed class RunOnceEnumerable<T> : IRunOnceEnumerable<T>
        {
            private IEnumerable<T> _enumerable;
            private IEnumerator<T> _enumerator;

            private int _indexedUpTo;
            private List<T> _data = new List<T>();

            public RunOnceEnumerable(IEnumerable<T> enumerable)
            {
                _enumerable = enumerable;
                _enumerator = enumerable.GetEnumerator();
            }

            public IEnumerator<T> GetEnumerator()
            {
                if (_enumerable != null)
                {
                    return GetRunOnceEnumerator();
                }
                else
                {
                    return _data.GetEnumerator();
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public List<T> ToList()
            {
                if (_enumerable != null)
                {
                    return Enumerable.ToList(this);
                }
                else
                {
                    return _data;
                }
            }

            private IEnumerator<T> GetRunOnceEnumerator()
            {
                int currentIndex = 0;
                bool isMoreData = true;
                do
                {
                    if (currentIndex < _indexedUpTo)
                    {
                        yield return _data[currentIndex];
                    }
                    else
                    {
                        if (isMoreData = _enumerator.MoveNext())
                        {
                            _indexedUpTo++;
                            _data.Add(_enumerator.Current);
                            yield return _enumerator.Current;
                        }
                        else
                        {   
                            // Delete the refernece to the enumerable now we have completed the enumeration, it won't be used again.
                            _enumerable = null;
                            _enumerator.Dispose();

                        }
                    }
                    currentIndex++;
                } while (isMoreData);
            }
        }

        private sealed class ConcurrentRunOnceEnumerable<T> : IRunOnceEnumerable<T>, ICollection<T>
        {
            private IEnumerator<T> _enumerator;
            private IList<T> _dataList;
            private ICollection<T> _dataCollection = new List<T>();

            private bool _isList = false;
            private bool _isCollection = false;
            private volatile int _indexedUpTo = 0;
            private volatile bool _gotAllData = false;

            public List<T> ToList()
            {
                if (_isList || _gotAllData)
                {
                    return _dataList.ToList();
                }
                else if (_isCollection)
                {
                    return _dataCollection.ToList();
                }
                else
                {
                    return Enumerable.ToList(this);
                }
            }

            public ConcurrentRunOnceEnumerable(IEnumerable<T> enumerable)
            {
                if (enumerable is IList<T>)
                {
                    _isList = true;
                    _dataList = (IList<T>)enumerable;
                }
                else if (enumerable is ICollection<T>)
                {
                    _isCollection = true;
                    _dataCollection = (ICollection<T>)enumerable;
                }
                else
                {
                    this._enumerator = enumerable.GetEnumerator();
                    _dataList = new List<T>();
                }
            }
            public IEnumerator<T> GetEnumerator()
            {
                if (_isList || _gotAllData)
                {
                    return _dataList.GetEnumerator();
                }
                else if (_isCollection)
                {
                    return _dataCollection.GetEnumerator();
                }
                else
                {
                    return GetRunOnceEnumerator();
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

            private IEnumerator<T> GetRunOnceEnumerator()
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

            int ICollection<T>.Count
            {
                get
                {
                    if (_isList || _gotAllData)
                    {
                        return _dataList.Count;
                    }
                    else if (_isCollection)
                    {
                        return _dataCollection.Count();
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
                if (_isList || _gotAllData)
                {
                    _dataList.CopyTo(array, arrayIndex);
                }
                else if (_isCollection)
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
    }
}
