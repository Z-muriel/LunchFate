namespace LunchFate.Shops;

/// <summary>
/// 午餐店铺分类（需求文档 §3.2 校验规则表）。
/// </summary>
public enum ShopCategory
{
    /// <summary>快餐</summary>
    FastFood = 0,

    /// <summary>盖饭</summary>
    RiceBowl = 1,

    /// <summary>面食</summary>
    Noodles = 2,

    /// <summary>米粉</summary>
    RiceNoodles = 3,

    /// <summary>日料</summary>
    Japanese = 4,

    /// <summary>西餐</summary>
    Western = 5,

    /// <summary>轻食</summary>
    LightMeal = 6,

    /// <summary>饮品</summary>
    Drinks = 7,

    /// <summary>其他</summary>
    Other = 8
}
