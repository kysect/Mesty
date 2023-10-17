using NUnit.Framework;

namespace Mesty.Tests;

public class InterlockedTests
{
    [Test]
    public void CompareExchange()
    {
        int value = 0;
        int result = Interlocked.CompareExchange(ref value, 1, 0);
        Assert.That(result, Is.EqualTo(0));
        Assert.That(value, Is.EqualTo(1));
    }

    [Test]
    public void CompareExchange2()
    {
        int value = 0;
        int result = Interlocked.CompareExchange(ref value, 2, 1);
        Assert.That(result, Is.EqualTo(0));
        Assert.That(value, Is.EqualTo(0));
    }
}