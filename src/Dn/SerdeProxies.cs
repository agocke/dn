using System.Collections.Immutable;
using Serde;

namespace Dn;

internal static class ImmutableDictionaryProxy
{
    private sealed class SerdeInfo<TKey, TValue, TKeyProvider, TValueProvider>
        where TKey : notnull
        where TKeyProvider : ISerializeProvider<TKey>
        where TValueProvider : ISerializeProvider<TValue>
    {
        public static readonly ISerdeInfo Instance = SerdeInfo.MakeDictionary(
            typeof(ImmutableDictionary<TKey, TValue>).ToString(),
            SerdeInfoProvider.GetSerializeInfo<TKey, TKeyProvider>(),
            SerdeInfoProvider.GetSerializeInfo<TValue, TValueProvider>()
        );
    }

    public sealed class Ser<TKey, TValue, TKeyProvider, TValueProvider>()
        : SerDictBase<
                Ser<TKey, TValue, TKeyProvider, TValueProvider>,
                TKey,
                TValue,
                ImmutableDictionary<TKey, TValue>,
                TKeyProvider,
                TValueProvider
            >,
            ISerializeProvider<ImmutableDictionary<TKey, TValue>>
        where TKey : notnull
        where TKeyProvider : ISerializeProvider<TKey>
        where TValueProvider : ISerializeProvider<TValue>
    {
        public override ISerdeInfo SerdeInfo => SerdeInfo<TKey, TValue, TKeyProvider, TValueProvider>.Instance;
    }
}
