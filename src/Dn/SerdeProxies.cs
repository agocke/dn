using System.Collections.Immutable;
using Serde;

namespace Dn;

internal static class ImmutableDictionaryProxy
{
    private sealed class SerdeInfo<TKey, TValue>
        where TKey : notnull
    {
        public static readonly ISerdeInfo Instance = SerdeInfo.MakeDictionary(
            typeof(ImmutableDictionary<TKey, TValue>).ToString()
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
        >(SerdeInfo<TKey, TValue>.Instance),
            ISerializeProvider<ImmutableDictionary<TKey, TValue>>
        where TKey : notnull
        where TKeyProvider : ISerializeProvider<TKey>
        where TValueProvider : ISerializeProvider<TValue> { }
}
