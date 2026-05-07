namespace FacShopAPI.Shared.Http;

public class IdempotencyKeyOptions
{
    public string HeaderName { get; set; } = "Idempotency-Key";
    public List<string> Methods { get; set; } = ["POST", "PUT", "PATCH"];
    public List<string> PathContains { get; set; } = [];
}
