namespace JanaticsApi.Application.Common.Abstractions;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery
{
    Task<Result<TResult>> Handle(
        TQuery request,
        CancellationToken cancellationToken);
}
