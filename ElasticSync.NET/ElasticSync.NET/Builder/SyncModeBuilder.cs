using ElasticSync.Models;


namespace ElasticSync.NET.Builder
{
    public class RealTimeSyncBuilder
    {
        private readonly ElasticSyncOptions _options;
        public RealTimeSyncBuilder(ElasticSyncOptions options) 
        {
            _options = options;
        }

        public ElasticSyncOptions EnableMultipleWorkers(WorkerOptions workerOptions)
        {
            _options.EnableMultipleWorkers(workerOptions);
            return _options;
        }

    }

    public class IntervalSyncBuilder
    {
        private readonly ElasticSyncOptions _options;
        public IntervalSyncBuilder(ElasticSyncOptions options)
        {
            _options = options;
        }

        public ElasticSyncOptions EnableMultipleWorkers(WorkerOptions workerOptions)
        {
            _options.EnableMultipleWorkers(workerOptions);
            return _options;
        }

    }
}
