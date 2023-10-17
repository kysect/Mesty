namespace Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;

public record IfSourceCodeStatementDeclaration(
    string LeftOperandName,
    string RightOperandName,
    int TrueBranchStatementCount) : ISourceCodeMethodStatementDeclaration
{
    public virtual bool Equals(IfSourceCodeStatementDeclaration? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (LeftOperandName != other.LeftOperandName)
            return false;

        if (RightOperandName != other.RightOperandName)
            return false;

        if (TrueBranchStatementCount != other.TrueBranchStatementCount)
            return false;

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LeftOperandName, RightOperandName, TrueBranchStatementCount);
    }
}