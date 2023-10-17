using Kysect.CommonLib.BaseTypes.Extensions;

namespace Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;

public record AutoResetEventSourceCodeVariableDeclaration(string Name, bool IsSet) : ISourceCodeVariableDeclaration
{
    public ISourceCodeVariableDeclaration UpdateValue(ISourceCodeVariableDeclaration newValue)
    {
        if (newValue is not AutoResetEventSourceCodeVariableDeclaration other)
            throw new ArgumentException(nameof(other));

        return new AutoResetEventSourceCodeVariableDeclaration(Name, other.IsSet);
    }

    public bool ValueEquals(ISourceCodeVariableDeclaration other)
    {
        return other.To<AutoResetEventSourceCodeVariableDeclaration>().IsSet == IsSet;
    }

    public override string ToString()
    {
        return $"AutoResetEvent '{Name}', IsSet: {IsSet}";
    }
}