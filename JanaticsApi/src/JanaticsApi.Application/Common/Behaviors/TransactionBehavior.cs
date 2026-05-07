// src/EnterpriseApi.Application/Common/Behaviors/TransactionBehavior.cs
using JanaticsApi.Application.Common.Abstractions;
using MediatR;

namespace JanaticsApi.Application.Common.Behaviors;

// Only wraps commands in transactions — queries run without transaction overhead
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand
    where TResponse : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork) =>
        _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            throw;
        }
    }
}