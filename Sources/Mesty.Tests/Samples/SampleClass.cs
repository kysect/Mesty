namespace Mesty.Tests.Samples;

// T1: Set()
// T1: Interlocked.Increment(ref setCount) | setCount = 1
// T2: Wait()
// T2: Interlocked.Read(ref setCount) => 1
// T2: currCount = 1
// T2: if (currCount == 0) => false
// T2: while (currCount == 0) => false
// T2: Interlocked.CompareExchange(ref setCount, currCount - 1, currCount) => true | setCount = 0 (currCount=1, setCount=1)
// T2: setCount = 0
// T3: 
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Test class for analysis")]
public class SampleClass
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

    public void Wait()
    {
        while (true)
        {
            long currCount = 0;
            long zeroValue = 0;
            while (currCount == zeroValue)
            {
                currCount = Interlocked.Read(ref _setCount);
                if (currCount == zeroValue)
                {
                    _notEmptyEvent.WaitOne();
                }
            }

            long decrementedValue = currCount - 1;
            long updatedValue;
            updatedValue = Interlocked.CompareExchange(ref _setCount, currCount, decrementedValue);
            if (updatedValue == currCount)
            {
                return;
            }
        }
    }
}