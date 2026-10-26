using System.Data;
using System.Text.Json.Serialization;
using Dapper;

namespace PersonalBudgetTrackerApi.Models;

public class Category
{
    public int CategoryId { get; set; }
    public Guid PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; } = CategoryType.expense;//default value
    public bool IsDefault { get; set; } = false;
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CategoryType
{
    income,
    expense
}

public class CategoryTypeEnumHandler: SqlMapper.TypeHandler<CategoryType>
{
    public override void SetValue(IDbDataParameter parameter, CategoryType value) => parameter.Value = value.ToString();
    public override CategoryType Parse(object value) => Enum.Parse<CategoryType>(value.ToString()!);
}