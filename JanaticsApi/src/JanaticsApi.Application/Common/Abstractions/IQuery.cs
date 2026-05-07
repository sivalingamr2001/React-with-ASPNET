// src/EnterpriseApi.Application/Common/Abstractions/IQuery.cs
using JanaticsApi.Application.Common.Models;
using MediatR;

namespace JanaticsApi.Application.Common.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{ }