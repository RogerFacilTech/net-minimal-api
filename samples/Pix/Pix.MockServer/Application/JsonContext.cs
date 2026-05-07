using System.Text.Json.Serialization;
using Pix.MockServer.Contracts;

namespace Pix.MockServer.Application;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CriarCobrancaRequest))]
internal partial class JsonContext : JsonSerializerContext
{
}
