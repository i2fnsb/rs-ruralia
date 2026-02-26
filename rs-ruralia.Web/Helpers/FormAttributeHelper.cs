using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace rs_ruralia.Web.Helpers;

/// <summary>
/// Helper class for extracting validation and display attributes from model properties
/// </summary>
public static class FormAttributeHelper
{
    /// <summary>
    /// Get the Display Name from [Display(Name = "...")] attribute
    /// </summary>
    public static string GetDisplayName<T>(Expression<Func<T, object>> propertyExpression)
    {
        var member = propertyExpression.Body as MemberExpression
            ?? (propertyExpression.Body is UnaryExpression unary ? unary.Operand as MemberExpression : null);

        if (member == null)
            throw new ArgumentException("Expression is not a member access", nameof(propertyExpression));

        var displayAttr = member.Member.GetCustomAttribute<DisplayAttribute>();
        return displayAttr?.Name ?? member.Member.Name;
    }

    /// <summary>
    /// Get the Description from [Display(Description = "...")] attribute
    /// </summary>
    public static string GetDescription<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var displayAttribute = memberInfo?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Description ?? string.Empty;
    }

    /// <summary>
    /// Get the MaxLength from [MaxLength(...)] attribute
    /// </summary>
    public static int GetMaxLength<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression); 
        var maxLengthAttribute = memberInfo?.GetCustomAttribute<MaxLengthAttribute>();
        return maxLengthAttribute?.Length ?? 5120;
    }

    /// <summary>
    /// Get the MinLength from [MinLength(...)] attribute
    /// </summary>
    public static int? GetMinLength<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var minLengthAttribute = memberInfo?.GetCustomAttribute<MinLengthAttribute>();
        return minLengthAttribute?.Length;
    }

    /// <summary>
    /// Check if property has [Required] attribute
    /// </summary>
    public static bool GetRequired<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var requiredAttribute = memberInfo?.GetCustomAttribute<RequiredAttribute>();
        return requiredAttribute != null;
    }

    /// <summary>
    /// Check if property has [EmailAddress] attribute
    /// </summary>
    public static bool GetEmail<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var emailAttribute = memberInfo?.GetCustomAttribute<EmailAddressAttribute>();
        return emailAttribute != null;
    }

    /// <summary>
    /// Check if property has [Phone] attribute
    /// </summary>
    public static bool GetPhone<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var phoneAttribute = memberInfo?.GetCustomAttribute<PhoneAttribute>();
        return phoneAttribute != null;
    }

    /// <summary>
    /// Check if property has [Url] attribute
    /// </summary>
    public static bool GetUrl<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var urlAttribute = memberInfo?.GetCustomAttribute<UrlAttribute>();
        return urlAttribute != null;
    }

    /// <summary>
    /// Get the Range (Min, Max) from [Range(...)] attribute
    /// </summary>
    public static (decimal? min, decimal? max) GetRange<T>(Expression<Func<T, object>> propertyExpression)  
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var rangeAttribute = memberInfo?.GetCustomAttribute<RangeAttribute>();
        
        if (rangeAttribute == null) 
            return (null, null);

        return (SafeConvertToDecimal(rangeAttribute.Minimum), SafeConvertToDecimal(rangeAttribute.Maximum));
    }

    /// <summary>
    /// Safely convert a value to decimal, clamping to decimal.MinValue/MaxValue if out of range
    /// </summary>
    private static decimal SafeConvertToDecimal(object value)   
    {
        if (value is double doubleValue)
        {
            if (doubleValue >= (double)decimal.MaxValue)
                return decimal.MaxValue;
            if (doubleValue <= (double)decimal.MinValue)
                return decimal.MinValue;
        }
        
        return Convert.ToDecimal(value);
    }

    /// <summary>
    /// Get the RegEx pattern from [RegularExpression(...)] attribute
    /// </summary>
    public static string? GetPattern<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var regexAttribute = memberInfo?.GetCustomAttribute<RegularExpressionAttribute>();
        return regexAttribute?.Pattern;
    }

    /// <summary>
    /// Get the DataType from [DataType(...)] attribute
    /// </summary>
    public static DataType? GetDataType<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var dataTypeAttribute = memberInfo?.GetCustomAttribute<DataTypeAttribute>();
        return dataTypeAttribute?.DataType;
    }

    /// <summary>
    /// Check if property has [ForeignKey(...)] attribute and get the related table/property
    /// </summary>
    public static (bool isForeignKey, string? relatedEntity) GetForeignKey<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        var foreignKeyAttribute = memberInfo?.GetCustomAttribute<ForeignKeyAttribute>();
        
        if (foreignKeyAttribute != null)
        {
            return (true, foreignKeyAttribute.Name);
        }
        
        return (false, null);
    }

    /// <summary>
    /// Check if property is a foreign key
    /// </summary>
    public static bool IsForeignKey<T>(Expression<Func<T, object>> propertyExpression)
    {
        var memberInfo = GetMemberInfo(propertyExpression);
        return memberInfo?.GetCustomAttribute<ForeignKeyAttribute>() != null;
    }

    /// <summary>
    /// Extract MemberInfo from property expression
    /// </summary>
    private static MemberInfo? GetMemberInfo<T>(Expression<Func<T, object>> expression)
    {
        var memberExpression = expression.Body as MemberExpression 
            ?? (expression.Body as UnaryExpression)?.Operand as MemberExpression;
        
        return memberExpression?.Member;
    }
}
