using Kysect.CommonLib.BaseTypes.Extensions;

namespace Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;

public record SimpleLongSourceCodeVariableDeclaration(string Name, long Value) : ISourceCodeVariableDeclaration
{
    public ISourceCodeVariableDeclaration UpdateValue(ISourceCodeVariableDeclaration newValue)
    {
        if (newValue is not SimpleLongSourceCodeVariableDeclaration other)
            throw new ArgumentException(nameof(other));

        return new SimpleLongSourceCodeVariableDeclaration(Name, other.Value);
    }

    public bool ValueEquals(ISourceCodeVariableDeclaration other)
    {
        return other.To<SimpleLongSourceCodeVariableDeclaration>().Value == Value;
    }

    public override string ToString()
    {
        return $"Int64 {Name}: {Value}";
    }
}

public record LongInitFromOtherSourceCodeVariableDeclaration(string Name, string OtherValue) : ISourceCodeVariableDeclaration
{
    public ISourceCodeVariableDeclaration UpdateValue(ISourceCodeVariableDeclaration newValue)
    {
        if (newValue is LongInitFromOtherSourceCodeVariableDeclaration other)
            return new LongInitFromOtherSourceCodeVariableDeclaration(Name, other.OtherValue);

        if (newValue is SimpleLongSourceCodeVariableDeclaration other2)
            return new SimpleLongSourceCodeVariableDeclaration(Name, other2.Value);

        throw new ArgumentException(nameof(other));

    }

    public bool ValueEquals(ISourceCodeVariableDeclaration other)
    {
        return other is LongInitFromOtherSourceCodeVariableDeclaration o;
    }
}
