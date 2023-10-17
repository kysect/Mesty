using Kysect.CommonLib.BaseTypes.Extensions;
using Mesty.Interpreter.Models;
using Mesty.SourceCodeDeclaration.Abstractions.Models.MethodStatements;
using Mesty.SourceCodeDeclaration.Abstractions.Models.Variables;
using Microsoft.Extensions.Logging;

namespace Mesty.Interpreter;

public interface IStatementExecutor
{
    bool CanExecute(SourceCodeExecutionThreadPointer threadPointer);
    SourceCodeExecutionThreadPointer ExecuteThreadAction(SourceCodeExecutionThreadPointer threadPointer);
}

public class StatementExecutor : IStatementExecutor
{
    private readonly ILogger _logger;
    private readonly WellKnownMethodExecutor _wellKnownMethodExecutor;

    public StatementExecutor(ILogger logger)
    {
        _logger = logger;
        _wellKnownMethodExecutor = new WellKnownMethodExecutor(logger);
    }

    public bool CanExecute(SourceCodeExecutionThreadPointer threadPointer)
    {
        threadPointer.ThrowIfNull();

        return threadPointer.MethodStack.Count > 0;
    }

    public SourceCodeExecutionThreadPointer ExecuteThreadAction(SourceCodeExecutionThreadPointer threadPointer)
    {
        threadPointer.ThrowIfNull();

        IReadOnlyCollection<SourceCodeExecutionMethodStatementPointer> threadStack = threadPointer.MethodStack;
        if (threadStack.Count == 0)
            throw new InvalidOperationException("Thread stack is empty. No method pointers.");

        SourceCodeExecutionMethodStatementPointer sourceCodeExecutionMethodStatementPointer = threadStack.Last();
        ISourceCodeMethodStatementDeclaration sourceCodeMethodStatementDeclaration = threadPointer.Context.GetStatement(sourceCodeExecutionMethodStatementPointer.MethodName, sourceCodeExecutionMethodStatementPointer.StatementIndex);

        return ProcessStatement(threadPointer, sourceCodeMethodStatementDeclaration);
    }

    private SourceCodeExecutionThreadPointer ProcessStatement(SourceCodeExecutionThreadPointer threadPointer, ISourceCodeMethodStatementDeclaration sourceCodeMethodStatementDeclaration)
    {
        _logger.LogInformation("Execute statement {statement} in thread {threadId}", sourceCodeMethodStatementDeclaration.GetType().Name, threadPointer.ThreadId);

        switch (sourceCodeMethodStatementDeclaration)
        {
            case VariableDeclarationSourceCodeMethodStatementDeclaration variableDeclaration:
                threadPointer = threadPointer.SetLocalVariable(variableDeclaration.Variable);
                return MovePointerToNextStatement(threadPointer);

            case InterlockedIncrementMethodStatementDeclaration interlockedIncrementMethodStatement:
                threadPointer = _wellKnownMethodExecutor.ProcessInterlockIncrement(threadPointer, interlockedIncrementMethodStatement);
                return MovePointerToNextStatement(threadPointer);
            case InterlockedReadMethodStatementDeclaration interlockedReadMethodStatement:
                threadPointer = _wellKnownMethodExecutor.ProcessInterlockRead(threadPointer, interlockedReadMethodStatement);
                return MovePointerToNextStatement(threadPointer);
            case InterlockedCompareExchangeMethodStatementDeclaration compareExchangeMethodStatement:
                threadPointer = _wellKnownMethodExecutor.ProcessInterlockCompareExchange(threadPointer, compareExchangeMethodStatement);
                return MovePointerToNextStatement(threadPointer);

            case SetValueToVariableStatementDeclaration setValueToVariableStatement:
                if (!long.TryParse(setValueToVariableStatement.VariableValue, out long result))
                    throw new NotImplementedException("Variable type is not supported: " + setValueToVariableStatement.VariableValue);

                var simpleLongSourceCodeVariableDeclaration = new SimpleLongSourceCodeVariableDeclaration(setValueToVariableStatement.VariableName, result);
                threadPointer = threadPointer.SetLocalOrGlobalVariable(setValueToVariableStatement.VariableName, simpleLongSourceCodeVariableDeclaration);
                return MovePointerToNextStatement(threadPointer);

            case SetValueToVariableWithDecrementStatementDeclaration setValueToVariableWithDecrementStatementDeclaration:
                ISourceCodeVariableDeclaration sourceCodeVariableDeclaration = threadPointer.GetLocalOrGlobalVariable(setValueToVariableWithDecrementStatementDeclaration.VariableValue);
                if (sourceCodeVariableDeclaration is not SimpleLongSourceCodeVariableDeclaration longValue)
                    throw new NotImplementedException("Only long is supported");

                threadPointer = threadPointer.SetLocalOrGlobalVariable(
                    setValueToVariableWithDecrementStatementDeclaration.VariableName,
                    longValue with { Value = longValue.Value - 1 });
                return MovePointerToNextStatement(threadPointer);

            case IfSourceCodeStatementDeclaration ifSourceCodeStatement:
            {
                ISourceCodeVariableDeclaration leftOperand = threadPointer.GetLocalOrGlobalVariable(ifSourceCodeStatement.LeftOperandName);
                ISourceCodeVariableDeclaration rightOperand = threadPointer.GetLocalOrGlobalVariable(ifSourceCodeStatement.RightOperandName);
                if (leftOperand.ValueEquals(rightOperand))
                {
                    _logger.LogInformation("if ({left} == {right}) condition is true. Move to body", leftOperand, rightOperand);
                    return MovePointerToNextStatement(threadPointer);
                }
                else
                {
                    _logger.LogInformation("if ({left} == {right}) condition is false. Skip {bodyStatementCount} statements.", leftOperand, rightOperand, ifSourceCodeStatement.TrueBranchStatementCount);
                    threadPointer = threadPointer.MovePointerToNextStatement(ifSourceCodeStatement.TrueBranchStatementCount);
                    return MovePointerToNextStatement(threadPointer);
                }
            }

            case WhileTrueSourceCodeStatementDeclaration whileTrueSourceCodeStatementDeclaration:
                return MovePointerToNextStatement(threadPointer);

            case WhileSourceCodeStatementDeclaration whileSourceCodeStatementDeclaration:
            {
                ISourceCodeVariableDeclaration leftOperand = threadPointer.GetLocalOrGlobalVariable(whileSourceCodeStatementDeclaration.LeftOperandName);
                ISourceCodeVariableDeclaration rightOperand = threadPointer.GetLocalOrGlobalVariable(whileSourceCodeStatementDeclaration.RightOperandName);
                if (leftOperand.ValueEquals(rightOperand))
                {
                    _logger.LogInformation("while ({left} == {right}) condition is true. Move to body", leftOperand, rightOperand);
                    return MovePointerToNextStatement(threadPointer);
                }
                else
                {
                    _logger.LogInformation("while ({left} == {right}) condition is false. Skip {bodyStatementCount} statements.", leftOperand, rightOperand, whileSourceCodeStatementDeclaration.BlockStatementCount);
                    threadPointer = threadPointer.MovePointerToNextStatement(whileSourceCodeStatementDeclaration.BlockStatementCount + 1);
                    return MovePointerToNextStatement(threadPointer);
                }
            }

            case SkipStatementSourceCodeMethodStatementDeclaration skipStatementSourceCodeMethodStatementDeclaration:
                _logger.LogInformation("Skip {count} declarations", skipStatementSourceCodeMethodStatementDeclaration.SkipLine);
                return threadPointer.MovePointerToNextStatement(skipStatementSourceCodeMethodStatementDeclaration.SkipLine);

            case OtherMethodInvocationStatementDeclaration otherMethodInvocationStatement:
                //TODO: it is not only type but variable
                if (threadPointer.TryGetLocalOrGlobalVariable(otherMethodInvocationStatement.TypeName, out ISourceCodeVariableDeclaration? variable))
                {
                    threadPointer = _wellKnownMethodExecutor.ExecuteVariableMethod(threadPointer, variable, otherMethodInvocationStatement);
                    return MovePointerToNextStatement(threadPointer);
                }

                throw new NotImplementedException($"OtherMethodInvocationStatementDeclaration is not supported. Statement: {otherMethodInvocationStatement}");

            case ReturnStatementSourceCodeMethodStatementDeclaration returnStatementSourceCodeMethodStatementDeclaration:
                return threadPointer.ReturnFromLastMethod();

            default:
                throw new ArgumentOutOfRangeException(sourceCodeMethodStatementDeclaration.GetType().ToString());
        }
    }

    private SourceCodeExecutionThreadPointer MovePointerToNextStatement(SourceCodeExecutionThreadPointer threadPointer)
    {
        SourceCodeExecutionMethodStatementPointer sourceCodeExecutionMethodStatementPointer = threadPointer.MethodStack.Last();
        bool hasNextStatement = threadPointer.Context.HasNextStatement(sourceCodeExecutionMethodStatementPointer.MethodName, sourceCodeExecutionMethodStatementPointer.StatementIndex);

        SourceCodeExecutionThreadPointer result;

        if (hasNextStatement)
        {
            result = threadPointer.MovePointerToNextStatement();
            _logger.LogInformation("Move thread {threadId} execution to next statement: {pointer}", result.ThreadId, result.MethodStack.Last());
        }
        else
        {
            SourceCodeExecutionMethodStatementPointer lastPointer = threadPointer.MethodStack.Last();
            result = threadPointer.ReturnFromLastMethod();
            _logger.LogInformation("Return from method {method} in thread {threadId}", lastPointer, result.ThreadId);
        }

        return result;
    }
}