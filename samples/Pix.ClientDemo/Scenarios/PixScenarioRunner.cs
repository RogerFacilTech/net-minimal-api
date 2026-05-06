using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pix.ClientDemo.Client;
using Pix.ClientDemo.Models;

namespace Pix.ClientDemo.Scenarios;

public sealed class PixScenarioRunner(PixProcessingClient client, ILogger<PixScenarioRunner> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Iniciando cenário didático PIX... ");

        var cobrancaRequest = BuildSampleCobrancaRequest();
        var cobranca = await client.CriarCobrancaAsync(cobrancaRequest, cancellationToken);
        logger.LogInformation("Cobrança criada. txid={Txid} status={Status}", cobranca.Txid, cobranca.Status);

        var consulta = await client.ObterCobrancaAsync(cobranca.Txid, cancellationToken);
        logger.LogInformation("Consulta cobrança. txid={Txid} status={Status}", consulta.Txid, consulta.Status);

        var liquidada = await client.SimularLiquidacaoAsync(cobranca.Txid, cancellationToken);
        logger.LogInformation("Liquidação simulada. txid={Txid} status={Status}", liquidada.Txid, liquidada.Status);

        var devolucaoRequest = new CriarDevolucaoRequest(
            liquidada.Txid,
            $"E2E{Guid.NewGuid():N}"[..20],
            25.75m,
            "ORIGINAL",
            "Ajuste operacional",
            new Dictionary<string, JsonElement>
            {
                ["origem"] = JsonDocument.Parse("\"client-demo\"").RootElement,
                ["tentativa"] = JsonDocument.Parse("1").RootElement
            });

        var devolucao = await client.CriarDevolucaoAsync(devolucaoRequest, cancellationToken);
        logger.LogInformation("Devolução criada. id={DevolucaoId} status={Status}", devolucao.DevolucaoId, devolucao.Status);

        var consultaDevolucao = await client.ObterDevolucaoAsync(devolucao.DevolucaoId, cancellationToken);

        Console.WriteLine();
        Console.WriteLine("===== RESUMO PIX DEMO =====");
        Console.WriteLine($"TXID: {liquidada.Txid}");
        Console.WriteLine($"Status cobrança final: {liquidada.Status}");
        Console.WriteLine($"Devolução: {consultaDevolucao.DevolucaoId}");
        Console.WriteLine($"Status devolução final: {consultaDevolucao.Status}");
        Console.WriteLine("===========================");
    }

    private static CriarCobrancaRequest BuildSampleCobrancaRequest()
    {
        return new CriarCobrancaRequest(
            new CalendarioRequest(3600),
            new DevedorRequest(
                "Cliente de Teste",
                "12345678901",
                new EnderecoRequest("Rua das Flores", "100", "Sao Paulo", "SP", "01001000")),
            new RecebedorRequest("Empresa Demo", "12345678", "0001", "123456", "CACC"),
            new ValorRequest(150.75m, 0m, 5m, 0m, 0m),
            "chave-pix-demo-123",
            "Pagamento referente ao pedido #12345",
            [
                new SplitRequest("Parceiro A", "98765432100", 30m, 20),
                new SplitRequest("Parceiro B", "11222333000181", 15m, 10)
            ],
            [
                new InfoAdicionalRequest("pedido", "12345"),
                new InfoAdicionalRequest("canal", "mobile")
            ],
            new Dictionary<string, JsonElement>
            {
                ["origemSistema"] = JsonDocument.Parse("\"erp\"").RootElement,
                ["prioridade"] = JsonDocument.Parse("\"alta\"").RootElement,
                ["tags"] = JsonDocument.Parse("[\"pix\",\"educacional\"]").RootElement
            });
    }
}
