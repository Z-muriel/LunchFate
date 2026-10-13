namespace LunchFate.Blazor.Client.Components.Ui;

/// <summary>
/// Nb 自有组件的语义色。取值与设计令牌中的状态色一一对应，
/// 语义固定不可跨用：Success 恒为"启用/成功"，Warning 恒为"停用/注意"，
/// Danger 恒为"删除/错误"，Primary 恒为主操作，Info 恒为提示。
/// </summary>
public enum NbColor
{
    /// <summary>中性：白底 + 黑边，用于次要操作与普通标签。</summary>
    Neutral,

    /// <summary>主色（明黄）：主操作按钮、强调标签。</summary>
    Primary,

    /// <summary>成功：启用状态、成功结果。</summary>
    Success,

    /// <summary>警示：停用状态、需注意的提示。</summary>
    Warning,

    /// <summary>危险：删除操作、错误提示。</summary>
    Danger,

    /// <summary>信息：一般性提示。</summary>
    Info
}

/// <summary>
/// Nb 自有组件的尺寸档位，对应 <c>--nb-*</c> 令牌定义的触控目标高度。
/// </summary>
public enum NbSize
{
    /// <summary>小号：用于表格行内与工具栏，最小高度 34px。</summary>
    Small,

    /// <summary>中号（默认）：最小高度 44px，满足触控目标要求。</summary>
    Medium,

    /// <summary>大号：最小高度 52px，用于空状态与登录页等主操作。</summary>
    Large
}
