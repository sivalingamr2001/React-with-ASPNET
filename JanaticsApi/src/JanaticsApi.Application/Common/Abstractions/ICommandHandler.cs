namespace JanaticsApi.Application.Common.Abstractions;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result<Unit>> Handle(
        TCommand request,
        CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand
{
    Task<Result<TResult>> Handle(
        TCommand request,
        CancellationToken cancellationToken);
}
