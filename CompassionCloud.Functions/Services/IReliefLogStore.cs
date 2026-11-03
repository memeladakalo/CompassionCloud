namespace CompassionCloud.Functions.Services
{
    /// <summary>Abstraction over the storage used to log relief updates (lets tests use a fake).</summary>
    public interface IReliefLogStore
    {
        Task SaveAsync(string json, CancellationToken cancellationToken = default);
    }
}
