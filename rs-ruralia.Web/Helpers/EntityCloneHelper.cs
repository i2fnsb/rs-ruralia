namespace rs_ruralia.Web.Helpers;

/// <summary>
/// Provides helper methods for cloning entities
/// </summary>
public static class EntityCloneHelper
{
    /// <summary>
    /// Creates a shallow clone of an entity by copying all properties
    /// This uses reflection for maximum flexibility
    /// </summary>
    public static TEntity ShallowClone<TEntity>(TEntity source) where TEntity : class, new()
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var clone = new TEntity();
        var properties = typeof(TEntity).GetProperties()
            .Where(p => p.CanRead && p.CanWrite);

        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(source);
                prop.SetValue(clone, value);
            }
            catch
            {
                // Skip properties that can't be copied
            }
        }

        return clone;
    }

    /// <summary>
    /// Validates if two entities are equivalent based on their ValidFrom timestamps
    /// </summary>
    public static bool AreVersionsEqual<TEntity>(
        TEntity? version1, 
        TEntity? version2, 
        Func<TEntity, DateTime?> getValidFrom)
        where TEntity : class
    {
        if (version1 == null || version2 == null)
            return version1 == version2;

        return getValidFrom(version1) == getValidFrom(version2);
    }
}
