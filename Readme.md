# Project Mesty

Mesty is PoC of tooling that try to calculate all possible states of code that executes by multiple threads.

For example, this code:

```csharp
public class SampleClass1
{
    private long _setCount;
    private readonly AutoResetEvent _notEmptyEvent = new(false);

    public void Set()
    {
        long newValue;
        newValue = Interlocked.Increment(ref _setCount);
        long tempValue = 1;
        if (newValue == tempValue)
        {
            _notEmptyEvent.Set();
        }
    }
}
```

can be invoked this way:

```csharp
for (int i = 0; i < 3; i++)
    Task.Run(() => classInstance.Set());
```

3 threads will try to execute `Increment` method and possible results of method class for different methods:
```
1. 1th - 1; 2th - 2; 3th - 3
2. 1th - 1; 2th - 3; 3th - 2
3. ...
```

Generating all combinations allow us to proof that code has deadlocks or lead to unexpected results.
