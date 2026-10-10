using System;
using System.ComponentModel.DataAnnotations;

namespace LunchFate.Shops;

/// <summary>
/// 校验数值必须是指定步进的整数倍（需求文档 §3.2：评分 1.0 ~ 5.0、0.5 步进）。
/// 数据注解同时作用于服务端自动校验与前端 <c>DataAnnotationsValidator</c>。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MultipleOfAttribute : ValidationAttribute
{
    private readonly decimal _step;

    /// <summary>
    /// 创建步进倍数校验特性。
    /// </summary>
    /// <param name="step">步进值，必须大于 0；示例：<c>[MultipleOf(0.5)]</c></param>
    public MultipleOfAttribute(double step)
    {
        _step = (decimal)step;
        ErrorMessage = $"The {{0}} field must be a multiple of {_step}.";
    }

    /// <summary>
    /// 执行校验：null（未填写）放行；否则要求值为步进的整数倍。
    /// 使用 decimal 运算，避免浮点误差导致 0.5 步进误判。
    /// </summary>
    /// <param name="value">待校验的属性值</param>
    /// <param name="validationContext">校验上下文</param>
    /// <returns>校验结果，通过时为 <c>ValidationResult.Success</c></returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (value == null)
        {
            return ValidationResult.Success;
        }

        if (Convert.ToDecimal(value) % _step == 0)
        {
            return ValidationResult.Success;
        }

        var memberName = validationContext.MemberName;
        return new ValidationResult(
            FormatErrorMessage(validationContext.DisplayName),
            memberName == null ? null : new[] { memberName });
    }
}
