// src/EnterpriseApi.Application/Common/Abstractions/ICommand.cs
using JanaticsApi.Application.Common.Models;
using MediatR;

namespace JanaticsApi.Application.Common.Abstractions;

public interface ICommand : IRequest<Result<Unit>> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

public interface ICommandHandler<TCommand>
    : IRequestHandler<TCommand, Result<Unit>>
    where TCommand : ICommand
{ }

public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{ }