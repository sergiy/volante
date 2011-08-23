namespace Volante
{
    /// <summary>
    /// Storage performing replication of changed pages to specified slave nodes.
    /// </summary>
    public interface ReplicationMasterStorage : IDatabase
    {
        /// <summary>
        /// Get number of currently available slave nodes
        /// </summary>
        /// <returns>number of online replication slaves</returns>
        int GetNumberOfAvailableHosts();
    }
}