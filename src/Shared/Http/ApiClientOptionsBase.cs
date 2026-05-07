namespace FacShopAPI.Shared.Http;

public class ApiClientOptionsBase
{
    public string BaseUrl { get; set; } = string.Empty;
    public int AttemptTimeoutSeconds { get; set; } = 5;
    public int TotalRequestTimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int BaseRetryDelaySeconds { get; set; } = 1;
    public int CircuitSamplingWindowSeconds { get; set; } = 30;
    public int CircuitBreakDurationSeconds { get; set; } = 15;
    public int CircuitMinimumThroughput { get; set; } = 5;
    public double CircuitFailureRatio { get; set; } = 0.5;
}
