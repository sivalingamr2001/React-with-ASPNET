using JanaticsApi.Domain.Entities.Users;
using JanaticsApi.Domain.Repositories;
using JanaticsApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JanaticsApi.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context) =>
        _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email.Value == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await _context.Users.AddAsync(user, cancellationToken);
}
