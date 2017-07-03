using System.Threading.Tasks;
using Nethereum.Web3.Accounts;

namespace Nethereum.DappHybrid
{

    public interface IRpcProviderRequest
    {
        string GetAsyncScript();
        string GetMethodName();
        bool SyncSupported { get; }
        string GetSyncScrypt();
        Task<string> SendRequestAsync(string methodParameters, string id, string url);
    }
}