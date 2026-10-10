using System;
using LunchFate.Localization;
using Volo.Abp.AspNetCore.Components;
using Volo.Abp.Http.Client;

namespace LunchFate.Blazor.Client;

/// <summary>
/// 项目统一的 Blazor 组件基类：固定本地化资源，并提供异常 → 用户提示的统一转换。
/// </summary>
public abstract class LunchFateComponentBase : AbpComponentBase
{
    /// <summary>
    /// 构造函数：把本地化资源固定为本项目的 <see cref="LunchFateResource"/>。
    /// </summary>
    protected LunchFateComponentBase()
    {
        LocalizationResource = typeof(LunchFateResource);
    }

    /// <summary>
    /// 把捕获的异常转换为可展示给用户的提示。
    /// 远程调用失败统一抛 <see cref="AbpRemoteCallException"/>：服务端已把业务异常
    /// （如 UserFriendlyException）的原文放入 <c>Error.Message</c>，内部错误则被框架替换为
    /// 通用文案，因此直接展示该消息不会泄露内部信息；
    /// 拿不到消息（如网络中断、非 JSON 响应）时回退到本地化兜底文案。
    /// </summary>
    /// <param name="ex">捕获到的异常</param>
    /// <returns>可直接展示的错误提示</returns>
    protected string ToUserMessage(Exception ex)
    {
        if (ex is AbpRemoteCallException remoteException &&
            !string.IsNullOrWhiteSpace(remoteException.Error?.Message))
        {
            return remoteException.Error!.Message!;
        }

        return L["Error:Unexpected"].Value;
    }
}
