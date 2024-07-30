namespace Application;

public class RateLimitOptions
{
    public Dictionary<string, RateLimitPolicy> Policies { get; set; }
}

public class RateLimitPolicy
{
    public int Limit { get; set; }
    public TimeSpan Period { get; set; }
}

