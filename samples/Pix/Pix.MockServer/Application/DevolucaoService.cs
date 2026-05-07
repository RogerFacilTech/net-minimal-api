using Pix.MockServer.Contracts;
using Pix.MockServer.Domain;

namespace Pix.MockServer.Application;

public interface IDevolucaoService
{
    DevolucaoResponse? Create(CriarDevolucaoRequest request, string correlationId);
    DevolucaoResponse? Get(string devolucaoId);
}

public sealed class DevolucaoService(
    ICobrancaRepository cobrancaRepository,
    IDevolucaoRepository devolucaoRepository,
    TimeProvider timeProvider) : IDevolucaoService
{
    public DevolucaoResponse? Create(CriarDevolucaoRequest request, string correlationId)
    {
        if (!cobrancaRepository.TryGet(request.Txid, out var cobranca) || cobranca is null)
        {
            return null;
        }

        if (cobranca.Status != CobrancaStatus.Concluida)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var devolucaoId = Guid.NewGuid().ToString("N");
        var entity = new DevolucaoEntity
        {
            DevolucaoId = devolucaoId,
            Request = request,
            SolicitadaEm = now,
            Status = DevolucaoStatus.EmProcessamento,
            LiquidadaEm = null,
            CorrelationId = correlationId,
            OrigemAtualizacao = "MOCK_SERVER"
        };

        entity.Status = DevolucaoStatus.Devolvida;
        entity.LiquidadaEm = now.AddSeconds(2);
        entity.OrigemAtualizacao = "SIMULADOR";

        devolucaoRepository.Upsert(entity);
        return ToResponse(entity);
    }

    public DevolucaoResponse? Get(string devolucaoId)
    {
        if (!devolucaoRepository.TryGet(devolucaoId, out var entity) || entity is null)
        {
            return null;
        }

        return ToResponse(entity);
    }

    private static DevolucaoResponse ToResponse(DevolucaoEntity entity)
        => new(
            entity.DevolucaoId,
            entity.Status,
            entity.SolicitadaEm,
            entity.LiquidadaEm,
            entity.Request.Motivo,
            new RastreabilidadeResponse(
                entity.Request.Txid,
                entity.Request.EndToEndId,
                entity.LiquidadaEm ?? entity.SolicitadaEm,
                entity.OrigemAtualizacao),
            entity.CorrelationId);
}
