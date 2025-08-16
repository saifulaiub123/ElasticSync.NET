using System.Threading;
using System.Threading.Tasks;

namespace ElasticSync.NET.Interface
{
    public interface IInstallerService
    {
        Task InstallAsync(CancellationToken ct);
    }
}
