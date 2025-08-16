using ElasticSync.NET.Enum;

namespace ElasticSync.NET.Interface
{
    public interface IDatabaseConfigurationHandler
    {
        void SetDatabaseProvider(DatabaseProvider provider, string connectionString);
    }
}
