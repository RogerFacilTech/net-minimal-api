using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Pix.MockServer.Contracts;
using Pix.MockServer.Domain;

namespace Pix.MockServer.Application;

public interface ICobrancaService
{
    CobrancaCreateOutcome Create(CriarCobrancaRequest request, string idempotencyKey, string correlationId, string baseUrl);
    CobrancaResponse? Get(string txid, string baseUrl);
    CobrancaResponse? SimularLiquidacao(string txid, string baseUrl);
}

public sealed record CobrancaCreateOutcome(CobrancaResponse Response, bool IsIdempotentReplay, bool IsConflict);

public sealed class CobrancaService(
    ICobrancaRepository cobrancaRepository,
    IIdempotencyRepository idempotencyRepository,
    TimeProvider timeProvider) : ICobrancaService
{
    public CobrancaCreateOutcome Create(CriarCobrancaRequest request, string idempotencyKey, string correlationId, string baseUrl)
    {
        var fingerprint = BuildFingerprint(request);
        if (idempotencyRepository.TryGet(idempotencyKey, out var existingEntry) && existingEntry is not null)
        {
            if (!string.Equals(existingEntry.Fingerprint, fingerprint, StringComparison.Ordinal))
            {
                return new CobrancaCreateOutcome(BuildConflictResponse(existingEntry.ResourceId, baseUrl), false, true);
            }

            var replay = Get(existingEntry.ResourceId, baseUrl)!;
            return new CobrancaCreateOutcome(replay, true, false);
        }

        var now = timeProvider.GetUtcNow();
        var txid = Guid.NewGuid().ToString("N")[..32];
        var expiresAt = now.AddSeconds(request.Calendario.Expiracao);

        var entity = new CobrancaEntity
        {
            Txid = txid,
            Request = request,
            CriadaEm = now,
            ExpiraEm = expiresAt,
            CorrelationId = correlationId,
            Status = CobrancaStatus.Ativa,
            HistoricoStatus =
            [
                new HistoricoStatusItem(now, CobrancaStatus.Ativa, "MOCK_SERVER")
            ]
        };

        cobrancaRepository.Upsert(entity);
        idempotencyRepository.Upsert(new IdempotencyEntry(idempotencyKey, fingerprint, txid, now));

        return new CobrancaCreateOutcome(ToResponse(entity, baseUrl), false, false);
    }

    public CobrancaResponse? Get(string txid, string baseUrl)
    {
        if (!cobrancaRepository.TryGet(txid, out var entity) || entity is null)
        {
            return null;
        }

        AtualizarExpiracao(entity);
        return ToResponse(entity, baseUrl);
    }

    public CobrancaResponse? SimularLiquidacao(string txid, string baseUrl)
    {
        if (!cobrancaRepository.TryGet(txid, out var entity) || entity is null)
        {
            return null;
        }

        AtualizarExpiracao(entity);
        if (entity.Status == CobrancaStatus.Ativa)
        {
            entity.Status = CobrancaStatus.Concluida;
            entity.HistoricoStatus.Add(new HistoricoStatusItem(timeProvider.GetUtcNow(), CobrancaStatus.Concluida, "SIMULADOR"));
            cobrancaRepository.Upsert(entity);
        }

        return ToResponse(entity, baseUrl);
    }

    private void AtualizarExpiracao(CobrancaEntity entity)
    {
        if (entity.Status != CobrancaStatus.Ativa)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        if (now <= entity.ExpiraEm)
        {
            return;
        }

        entity.Status = CobrancaStatus.Expirada;
        entity.HistoricoStatus.Add(new HistoricoStatusItem(now, CobrancaStatus.Expirada, "SCHEDULER"));
        cobrancaRepository.Upsert(entity);
    }

    private static CobrancaResponse ToResponse(CobrancaEntity entity, string baseUrl)
    {
        var location = $"{baseUrl.TrimEnd('/')}/pix/v1/cobrancas/{entity.Txid}";
        var historico = entity.HistoricoStatus
            .Select(x => new HistoricoStatusResponse(x.DataHora, x.Status, x.Origem))
            .ToArray();

        return new CobrancaResponse(
            entity.Txid,
            entity.Status,
            entity.CriadaEm,
            entity.ExpiraEm,
            location,
            $"0002012636pix.mock/{entity.Txid}5204000053039865802BR5920RECEBEDOR DEMO6009SAO PAULO62070503***6304ABCD",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"PIX|{entity.Txid}|{entity.Request.Valor.Original:F2}")),
            historico,
            entity.Request.Valor.Original,
            entity.CorrelationId);
    }

    private static string BuildFingerprint(CriarCobrancaRequest request)
    {
        var json = JsonSerializer.Serialize(request, JsonContext.Default.CriarCobrancaRequest);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }

    private static CobrancaResponse BuildConflictResponse(string txid, string baseUrl)
        => new(
            txid,
            "CONFLICT",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            $"{baseUrl.TrimEnd('/')}/pix/v1/cobrancas/{txid}",
            string.Empty,
            string.Empty,
            Array.Empty<HistoricoStatusResponse>(),
            0,
            string.Empty);
}
