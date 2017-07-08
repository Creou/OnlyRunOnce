# Only Run Once

A C# `IEnumerable<T>` extension method that ensures that the enumerable is only evaluated once.

Usage:

    var willOnlyRunOnce = somethingIEnumerable.OnlyRunOnce();

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
 