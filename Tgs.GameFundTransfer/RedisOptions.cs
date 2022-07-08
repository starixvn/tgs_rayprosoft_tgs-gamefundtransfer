namespace Tgs.GameFundTransfer
{
    public record RedisOptions
    {
        public string ConnectionString { get; set; } = "localhost";
    }
}
