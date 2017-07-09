# Only Run Once

A C# `IEnumerable<T>` extension method that ensures that the enumerable is only evaluated once.

If you just want to grab the extension and don't care about the rest of project, it's all in one place: [RunOnceEnumerableExtensions.cs](https://github.com/Creou/OnlyRunOnce/blob/master/Creou.OnlyRunOnce/RunOnceEnumerableExtensions.cs)

Usage:

    var willOnlyRunOnce = somethingIEnumerable.OnlyRunOnce();
    var willOnlyRunOnce = somethingIEnumerable.OnlyRunOnceConcurrentSafe();

Calling `OnlyRunOnce()` on an `IEnumerable<T>` will return the enumerable wrapped by an `IRunOnceEnumerable<T>` that will only ever be evaluated once regardless of how many times you enumerate it.

Useful when you have some Linq statements that you want to be evaluated lazily, but you also want to prevent later code from repeated evaluating them unnecessarily. 

For example:

    List<string> listOfStrings = new List<string>() { "1", "2", "3" };
    var enumerableOfInts = listOfStrings.Select(s => int.Parse(s))
                                        .OnlyRunOnce();
    
    // No matter how many times you enumerate the ints now, the enumerable will only be evaulated once.
    foreach (var item in enumerableOfInts)
    {
        Console.WriteLine(item);
    }
    foreach (var item in enumerableOfInts)
    {
        Console.WriteLine(item);
    }
 
There is also a concurrent version that is entirely thread safe:

    List<string> listOfStrings = new List<string>() { "1", "2", "3" };
    var enumerableOfInts = listOfStrings.Select(s => int.Parse(s))
                                        .OnlyRunOnceConcurrentSafe();

    // No matter how many times you enumerate the ints even on multiple threads, the enumerable will only be evaulated once.
    await Task.WhenAll(
        Task.Run(() =>
        {
            foreach (var item in enumerableOfInts)
            {
                Console.WriteLine(item);
            }
        }),
        Task.Run(() =>
        {
            foreach (var item in enumerableOfInts)
            {
                Console.WriteLine(item);
            }
        }));