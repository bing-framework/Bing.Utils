namespace Bing.DependencyInjection;

/// <summary>同一服务类型与实现类型组合的注册策略，沿用原生容器的实现类型及服务键识别规则。</summary>
public enum ServiceRegistrationStrategy
{
    /// <summary>跳过重复组合，保留已有生命周期。</summary>
    SkipDuplicates,
    /// <summary>保留全部注册，包括重复组合。</summary>
    Append,
    /// <summary>遇到已有或本批重复组合时，拒绝整个批次。</summary>
    ThrowOnDuplicate
}
