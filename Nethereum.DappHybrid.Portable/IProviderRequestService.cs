using System.Threading.Tasks;
using Nethereum.Web3.Accounts;

namespace Nethereum.DappHybrid
{
    public interface IProviderRequestService
    {
        IAccount Account { get; }
        string RpcUrl { get; }

        string GetNethereumEmbeddedProviderScript();
        string GetWeb3InitScript();
        string GetWeb3Script();
        void RegisterProviderRequest(IRpcProviderRequest rpcProviderRequest);
        Task<string> SendRequestAsync(string data);
    }
}