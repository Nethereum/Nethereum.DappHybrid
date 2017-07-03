using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nethereum.DappHybrid
{
    public class ProviderRequestService : IProviderRequestService
    {
        private Dictionary<string,IRpcProviderRequest> requestsRegistry;

        public ProviderRequestService(string rpcUrl, IAccount account)
        {
            this.requestsRegistry = new Dictionary<string, IRpcProviderRequest>();
            this.RpcUrl = rpcUrl;
            this.Account = account;
        }

        public void RegisterProviderRequest(IRpcProviderRequest rpcProviderRequest)
        {
            requestsRegistry[rpcProviderRequest.GetMethodName()] = rpcProviderRequest;
            var requestAccount = rpcProviderRequest as IRpcRequestRequiredAccount;

            if (requestAccount != null)
            {
                if (Account == null) throw new Exception("Account not set");
                requestAccount.Account = Account;
            }
        }

        public string GetAsyncScriptsProviderRequests()
        {
            var stringBuilder = new StringBuilder();
            foreach (var request in requestsRegistry)
            {
                stringBuilder.AppendLine(request.Value.GetAsyncScript());
            }

            return stringBuilder.ToString();
        }

        public string GetSyncScriptsProviderRequests()
        {
            var stringBuilder = new StringBuilder();
            foreach (var request in requestsRegistry)
            {
                if (request.Value.SyncSupported)
                {
                    stringBuilder.AppendLine(request.Value.GetSyncScrypt());
                }
                else
                {
                    stringBuilder.AppendLine(Scripts.GetSyncScriptException(request.Value.GetMethodName()));
                }
            }
            return stringBuilder.ToString();
        }

        public IAccount Account { get; }

        public string RpcUrl { get; }
        
        public async Task<string> SendRequestAsync(string data)
        {
            var methodParams = data.Split('|');

            if (requestsRegistry.ContainsKey(methodParams[0])){

                var request = requestsRegistry[methodParams[0]];

                if (methodParams.Length > 2)
                {
                    return await request.SendRequestAsync(methodParams[2], methodParams[1], RpcUrl);
                }
                else
                {
                    return await request.SendRequestAsync(null, methodParams[1], RpcUrl);
                }
            }
            return null;
        }

        public string GetNethereumEmbeddedProviderScript()
        {
            return Scripts.GetNethereumEmbeddedProvider(GetAsyncScriptsProviderRequests(), GetSyncScriptsProviderRequests());
        }

        public string GetWeb3Script()
        {
            return Scripts.Web3Min;
        }

        public string GetWeb3InitScript()
        {
            return Scripts.GetInitWeb3(RpcUrl);
        }
    }
}
