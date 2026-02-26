using Dapper;
using System.Data;

namespace rs_ruralia.Web.Data;

/// <summary>
/// Example repository using Dapper for data access.
/// Inject IDbConnection to use this in your components or services.
/// </summary>
public class DapperRepository
{
    private readonly IDbConnection _db;

    public DapperRepository(IDbConnection db)
    {
        _db = db;
    }

    /// <summary>
    /// Example: Query all records from a table
    /// </summary>
    public async Task<IEnumerable<T>> GetAllAsync<T>(string tableName)
    {
        var sql = $"SELECT * FROM {tableName}";
        return await _db.QueryAsync<T>(sql);
    }

    /// <summary>
    /// Example: Query a single record by ID
    /// </summary>
    public async Task<T?> GetByIdAsync<T>(string tableName, int id)
    {
        var sql = $"SELECT * FROM {tableName} WHERE Id = @Id";
        return await _db.QuerySingleOrDefaultAsync<T>(sql, new { Id = id });
    }

    /// <summary>
    /// Example: Execute a stored procedure
    /// </summary>
    public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync<T>(string procedureName, object? parameters = null)
    {
        return await _db.QueryAsync<T>(
            procedureName,
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    /// <summary>
    /// Example: Insert a record
    /// </summary>
    public async Task<int> InsertAsync<T>(string tableName, T entity)
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id")
            .Select(p => p.Name);

        var columns = string.Join(", ", properties);
        var values = string.Join(", ", properties.Select(p => $"@{p}"));

        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT CAST(SCOPE_IDENTITY() as int)";
        return await _db.ExecuteScalarAsync<int>(sql, entity);
    }

    /// <summary>
    /// Example: Update a record
    /// </summary>
    public async Task<int> UpdateAsync<T>(string tableName, T entity, int id)
    {
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id")
            .Select(p => $"{p.Name} = @{p.Name}");

        var setClause = string.Join(", ", properties);
        var sql = $"UPDATE {tableName} SET {setClause} WHERE Id = @Id";

        var parameters = new DynamicParameters(entity);
        parameters.Add("Id", id);

        return await _db.ExecuteAsync(sql, parameters);
    }

    /// <summary>
    /// Example: Delete a record
    /// </summary>
    public async Task<int> DeleteAsync(string tableName, int id)
    {
        var sql = $"DELETE FROM {tableName} WHERE Id = @Id";
        return await _db.ExecuteAsync(sql, new { Id = id });
    }

    /// <summary>
    /// Example: Custom query
    /// </summary>
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        return await _db.QueryAsync<T>(sql, parameters);
    }

    /// <summary>
    /// Example: Execute non-query command
    /// </summary>
    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        return await _db.ExecuteAsync(sql, parameters);
    }
}
