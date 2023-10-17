using Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;
using NUnit.Framework;

namespace Mesty.Tests.Tools;

public class ExecutionStatementComparator
{
    public void CompareStatements(IReadOnlyList<ISourceCodeMethodStatementDeclaration> expected, IReadOnlyList<ISourceCodeMethodStatementDeclaration> actual)
    {
        int minCount = Math.Min(expected.Count, actual.Count);

        for (int i = 0; i < minCount; i++)
            Assert.That(actual[i], Is.EqualTo(expected[i]), $"Unexpected declaration on index {i}");

        Assert.That(expected.Count, Is.EqualTo(actual.Count));
    }
}