using Dapper;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace rs_ruralia.Web.Data;

public class DapperUserStore : IUserStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>, 
    IUserEmailStore<ApplicationUser>, IUserRoleStore<ApplicationUser>
{
    private readonly IDbConnection _connection;

    public DapperUserStore(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
                EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, 
                PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount)
            VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, 
                @EmailConfirmed, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @PhoneNumber, 
                @PhoneNumberConfirmed, @TwoFactorEnabled, @LockoutEnd, @LockoutEnabled, @AccessFailedCount)";

        user.Id = Guid.NewGuid().ToString();
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        
        await _connection.ExecuteAsync(sql, user);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM AspNetUsers WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new { user.Id });
        return IdentityResult.Success;
    }

    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT * FROM AspNetUsers WHERE Id = @userId";
        return await _connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new { userId });
    }

    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        const string sql = "SELECT * FROM AspNetUsers WHERE NormalizedUserName = @normalizedUserName";
        return await _connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new { normalizedUserName });
    }

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE AspNetUsers SET 
                UserName = @UserName, NormalizedUserName = @NormalizedUserName, 
                Email = @Email, NormalizedEmail = @NormalizedEmail, EmailConfirmed = @EmailConfirmed,
                PasswordHash = @PasswordHash, SecurityStamp = @SecurityStamp, ConcurrencyStamp = @ConcurrencyStamp,
                PhoneNumber = @PhoneNumber, PhoneNumberConfirmed = @PhoneNumberConfirmed,
                TwoFactorEnabled = @TwoFactorEnabled, LockoutEnd = @LockoutEnd, LockoutEnabled = @LockoutEnabled,
                AccessFailedCount = @AccessFailedCount
            WHERE Id = @Id";

        await _connection.ExecuteAsync(sql, user);
        return IdentityResult.Success;
    }

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        const string sql = "SELECT * FROM AspNetUsers WHERE NormalizedEmail = @normalizedEmail";
        return await _connection.QueryFirstOrDefaultAsync<ApplicationUser>(sql, new { normalizedEmail });
    }

    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        const string getRoleSql = "SELECT Id FROM AspNetRoles WHERE NormalizedName = @normalizedName";
        var roleId = await _connection.QueryFirstOrDefaultAsync<string>(getRoleSql, new { normalizedName = roleName.ToUpperInvariant() });

        if (roleId != null)
        {
            const string sql = "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
            await _connection.ExecuteAsync(sql, new { UserId = user.Id, RoleId = roleId });
        }
    }

    public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        const string getRoleSql = "SELECT Id FROM AspNetRoles WHERE NormalizedName = @normalizedName";
        var roleId = await _connection.QueryFirstOrDefaultAsync<string>(getRoleSql, new { normalizedName = roleName.ToUpperInvariant() });

        if (roleId != null)
        {
            const string sql = "DELETE FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId";
            await _connection.ExecuteAsync(sql, new { UserId = user.Id, RoleId = roleId });
        }
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT r.Name 
            FROM AspNetRoles r
            INNER JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
            WHERE ur.UserId = @UserId";

        var roles = await _connection.QueryAsync<string>(sql, new { UserId = user.Id });
        return roles.ToList();
    }

    public async Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        var roles = await GetRolesAsync(user, cancellationToken);
        return roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT u.* 
            FROM AspNetUsers u
            INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
            INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
            WHERE r.NormalizedName = @normalizedName";

        var users = await _connection.QueryAsync<ApplicationUser>(sql, new { normalizedName = roleName.ToUpperInvariant() });
        return users.ToList();
    }

    public void Dispose()
    {
        // Connection is managed by DI, don't dispose here
    }
}