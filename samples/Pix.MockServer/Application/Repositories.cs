using Pix.MockServer.Domain;

namespace Pix.MockServer.Application;

public interface ICobrancaRepository
{
    bool TryGet(string txid, out CobrancaEntity? cobranca);
    CobrancaEntity Upsert(CobrancaEntity cobranca);
}

public interface IDevolucaoRepository
{
    bool TryGet(string devolucaoId, out DevolucaoEntity? devolucao);
    DevolucaoEntity Upsert(DevolucaoEntity devolucao);
}

public interface IIdempotencyRepository
{
    bool TryGet(string key, out IdempotencyEntry? entry);
    IdempotencyEntry Upsert(IdempotencyEntry entry);
}

public sealed record IdempotencyEntry(string Key, string Fingerprint, string ResourceId, DateTimeOffset CreatedAt);
