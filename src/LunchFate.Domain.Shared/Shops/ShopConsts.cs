namespace LunchFate.Shops;

/// <summary>
/// 店铺字段长度约束（需求文档 §3.2 校验规则表）。
/// </summary>
public static class ShopConsts
{
    /// <summary>店铺名称最大长度。</summary>
    public const int MaxNameLength = 64;

    /// <summary>地址最大长度。</summary>
    public const int MaxAddressLength = 256;

    /// <summary>联系电话最大长度。</summary>
    public const int MaxPhoneLength = 32;

    /// <summary>单个标签的最大长度。</summary>
    public const int MaxTagLength = 12;

    /// <summary>标签串（逗号分隔）的最大长度。</summary>
    public const int MaxTagsLength = 256;

    /// <summary>备注最大长度。</summary>
    public const int MaxRemarkLength = 512;

    /// <summary>标签数量上限。</summary>
    public const int MaxTagsCount = 5;
}
