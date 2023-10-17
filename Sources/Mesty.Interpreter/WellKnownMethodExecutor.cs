using Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;
using Microsoft.Extensions.Logging;

namespace Mesty.Interpreter;

public class WellKnownMethodExecutor
{
    private readonly ILogger _logger;

    public WellKnownMethodExecutor(ILogger logger)
    {
        _logger = logger;
    }

    public SourceCodeExecutionThreadPointer ExecuteVariableMethod(
        SourceCodeExecutionThreadPointer threadPointer,
        ISourceCodeVariableDeclaration variable,
        OtherMethodInvocationStatementDeclaration otherMethodInvocationStatement)
    {
        switch (variable)
        {
            case AutoResetEventSourceCodeVariableDeclaration autoResetEvent:
                if (otherMethodInvocationStatement.MethodName == "Set")
                {
                    _logger.LogInformation("Execute AutoResetEvent.Set method for {variable}", autoResetEvent.Name);
                    threadPointer = threadPointer.SetLocalOrGlobalVariable(autoResetEvent.Name, autoResetEvent with { IsSet = true });
                    return threadPointer;
                }
                break;
        }

        throw new NotImplementedException($"OtherMethodInvocationStatementDeclaration is not supported. Statement: {otherMethodInvocationStatement}");
    }

    public SourceCodeExecutionThreadPointer ProcessInterlockIncrement(SourceCodeExecutionThreadPointer threadPointer, InterlockedIncrementMethodStatementDeclaration interlockedIncrementMethodStatement)
    {
        ISourceCodeVariableDeclaration oldValue = threadPointer.GetLocalOrGlobalVariable(interlockedIncrementMethodStatement.VariableName);
        ISourceCodeVariableDeclaration resultValue = IncrementValue(oldValue);
        threadPointer = threadPointer.SetLocalOrGlobalVariable(interlockedIncrementMethodStatement.VariableName, resultValue);
        _logger.LogInformation("Interlock incremented value for {variableName}", interlockedIncrementMethodStatement.VariableName);
        threadPointer = threadPointer.SetLocalOrGlobalVariable(interlockedIncrementMethodStatement.ResultVariableName, resultValue);
        _logger.LogInformation("Interlock incremented result set into {variableName}", interlockedIncrementMethodStatement.ResultVariableName);
        return threadPointer;
    }

    public SourceCodeExecutionThreadPointer ProcessInterlockRead(SourceCodeExecutionThreadPointer threadPointer, InterlockedReadMethodStatementDeclaration interlockedReadMethodStatement)
    {
        _logger.LogInformation("Interlock read value from {variableName}", interlockedReadMethodStatement.VariableName);
        ISourceCodeVariableDeclaration readVariable = threadPointer.GetLocalOrGlobalVariable(interlockedReadMethodStatement.VariableName);
        threadPointer = threadPointer.SetLocalOrGlobalVariable(interlockedReadMethodStatement.ResultVariableName, readVariable);

        return threadPointer;
    }

    public SourceCodeExecutionThreadPointer ProcessInterlockCompareExchange(SourceCodeExecutionThreadPointer threadPointer, InterlockedCompareExchangeMethodStatementDeclaration compareExchangeMethodStatement)
    {
        _logger.LogInformation("Interlock CompareExchange from {variableName}", compareExchangeMethodStatement.Location);

        ISourceCodeVariableDeclaration locationValue = threadPointer.GetLocalOrGlobalVariable(compareExchangeMethodStatement.Location);
        ISourceCodeVariableDeclaration comparandValue = threadPointer.GetLocalOrGlobalVariable(compareExchangeMethodStatement.Comparand);
        ISourceCodeVariableDeclaration valueValue = threadPointer.GetLocalOrGlobalVariable(compareExchangeMethodStatement.Value);
        ISourceCodeVariableDeclaration returnVariable = threadPointer.GetLocalOrGlobalVariable(compareExchangeMethodStatement.ResultVariableName);

        if (locationValue.ValueEquals(comparandValue))
        {
            _logger.LogInformation("Location and comparand value is same. Update location value");
            threadPointer.SetLocalOrGlobalVariable(compareExchangeMethodStatement.Location, valueValue);
        }
        else
        {
            _logger.LogInformation("Locatin value is not equals to comparand. Skip exchange.");
        }

        threadPointer.SetLocalOrGlobalVariable(compareExchangeMethodStatement.ResultVariableName, locationValue);

        return threadPointer;
    }

    public ISourceCodeVariableDeclaration IncrementValue(ISourceCodeVariableDeclaration variableValue)
    {
        return variableValue switch
        {
            SimpleLongSourceCodeVariableDeclaration longVariable => longVariable with { Value = longVariable.Value + 1 },
            _ => throw new NotImplementedException($"Variables with type {variableValue.GetType()} is not supported.")
        };
    }
}