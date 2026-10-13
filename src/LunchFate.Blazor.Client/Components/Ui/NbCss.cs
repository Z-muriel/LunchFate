using System.Collections.Generic;
using System.Linq;

namespace LunchFate.Blazor.Client.Components.Ui;

/// <summary>
/// Nb 自有组件的类名拼装工具：把基础类、变体修饰类与调用方附加类按顺序拼成一个 class 值，
/// 并过滤空值，避免在标记里散落三元表达式。
/// </summary>
internal static class NbCss
{
    /// <summary>
    /// 拼接非空类名，用单个空格分隔。
    /// </summary>
    /// <param name="parts">按顺序排列的类名片段，允许为 null 或空串</param>
    /// <returns>拼装后的 class 值；全部为空时返回空串</returns>
    public static string Join(params string?[] parts)
        => string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)));

    /// <summary>
    /// 生成 BEM-lite 变体类名，如 <c>nb-badge--success</c>。
    /// </summary>
    /// <param name="baseClass">基础类名，如 nb-badge</param>
    /// <param name="modifier">修饰名，如 success</param>
    /// <returns>变体类名</returns>
    public static string Modifier(string baseClass, string modifier) => $"{baseClass}--{modifier}";

    /// <summary>
    /// 把 <see cref="NbColor"/> 转成小写修饰名，与 components.css 中的类名一一对应。
    /// </summary>
    /// <param name="color">语义色</param>
    /// <returns>小写修饰名，如 primary</returns>
    public static string ColorName(NbColor color) => color.ToString().ToLowerInvariant();

    /// <summary>
    /// 把 <see cref="NbSize"/> 转成小写修饰名。
    /// </summary>
    /// <param name="size">尺寸档位</param>
    /// <returns>小写修饰名，如 small</returns>
    public static string SizeName(NbSize size) => size.ToString().ToLowerInvariant();
}
