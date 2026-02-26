using Dapper;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace rs_ruralia.Web.Data;

public class DapperRoleStore : IRoleStore<IdentityRole>
{
    private readonly IDbConnection _connection;

    public DapperRoleStore(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
            VALUES (@Id, @Name, @NormalizedName, @ConcurrencyStamp)";

        role.Id = Guid.NewGuid().ToString();
        role.ConcurrencyStamp = Guid.NewGuid().ToString();

        await _connection.ExecuteAsync(sql, role);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM AspNetRoles WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new { role.Id });
        return IdentityResult.Success;
    }

    public async Task<IdentityRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT * FROM AspNetRoles WHERE Id = @roleId";
        return await _connection.QueryFirstOrDefaultAsync<IdentityRole>(sql, new { roleId });
    }

    public async Task<IdentityRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        const string sql = "SELECT * FROM AspNetRoles WHERE NormalizedName = @normalizedRoleName";
        return await _connection.QueryFirstOrDefaultAsync<IdentityRole>(sql, new { normalizedRoleName });
    }

    public Task<string?> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.NormalizedName);
    }

    public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Id);
    }

    public Task<string?> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Name);
    }

    public Task SetNormalizedRoleNameAsync(IdentityRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(IdentityRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE AspNetRoles SET 
                Name = @Name, NormalizedName = @NormalizedName, ConcurrencyStamp = @ConcurrencyStamp
            WHERE Id = @Id";

        await _connection.ExecuteAsync(sql, role);
        return IdentityResult.Success;
    }

    public void Dispose()
    {
        // Connection is managed by DI, don't dispose here
    }
}