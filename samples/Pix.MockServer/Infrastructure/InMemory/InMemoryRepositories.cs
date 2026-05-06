using System.Collections.Concurrent;
using Pix.MockServer.Application;
using Pix.MockServer.Domain;

namespace Pix.MockServer.Infrastructure.InMemory;

public sealed class InMemoryCobrancaRepository : ICobrancaRepository
{
    private readonly ConcurrentDictionary<string, CobrancaEntity> _store = new(StringComparer.Ordinal);

    public bool TryGet(string txid, out CobrancaEntity? cobranca)
    {
        var found = _store.TryGetValue(txid, out var value);
        cobranca = value;
        return found;
    }

    public CobrancaEntity Upsert(CobrancaEntity cobranca)
    {
        _store[cobranca.Txid] = cobranca;
        return cobranca;
    }
}

public sealed class InMemoryDevolucaoRepository : IDevolucaoRepository
{
    private readonly ConcurrentDictionary<string, DevolucaoEntity> _store = new(StringComparer.Ordinal);

    public bool TryGet(string devolucaoId, out DevolucaoEntity? devolucao)
    {
        var found = _store.TryGetValue(devolucaoId, out var value);
        devolucao = value;
        return found;
    }

    public DevolucaoEntity Upsert(DevolucaoEntity devolucao)
    {
        _store[devolucao.DevolucaoId] = devolucao;
        return devolucao;
    }
}

public sealed class InMemoryIdempotencyRepository : IIdempotencyRepository
{
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _store = new(StringComparer.Ordinal);

    public bool TryGet(string key, out IdempotencyEntry? entry)
    {
        var found = _store.TryGetValue(key, out var value);
        entry = value;
        return found;
    }

    public IdempotencyEntry Upsert(IdempotencyEntry entry)
    {
        _store[entry.Key] = entry;
        return entry;
    }
}
