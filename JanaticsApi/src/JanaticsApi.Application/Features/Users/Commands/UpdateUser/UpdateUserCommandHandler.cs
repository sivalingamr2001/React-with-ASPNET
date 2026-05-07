using JanaticsApi.Application.Common.Abstractions;
using JanaticsApi.Application.Common.Caching;
using JanaticsApi.Application.Common.Models;
using JanaticsApi.Domain.Repositories;
using JanaticsApi.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace JanaticsApi.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Result<Unit>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<Unit>.Failure(Error.NotFound("User.NotFound", $"User {request.UserId} not found."));
        }

        user.UpdateProfile(request.FirstName, request.LastName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.Users.ById(request.UserId), cancellationToken),
            _cache.RemoveByPatternAsync(CacheKeys.Users.Pattern, cancellationToken));

        return Result<Unit>.Success(Unit.Value);
    }
}
