using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nethereum.DappHybrid
{

    public class EthAccountsProviderRequest: IRpcProviderRequest, IRpcRequestRequiredAccount
    {
        public string GetMethodName()
        {
            return "eth_accounts";
        }

        public virtual string GetAsyncScript()
        {
           return @"
               if (payload.method === 'eth_accounts') {
                handledAsync = true;
                var id = Math.random();
                currentRequestCallback[id] = respond;
                sendRequestToDotNetHost('eth_accounts|' + id);
               }";
        }

        public IAccount Account { get; set; }

        public virtual bool SyncSupported => true;

        public virtual async Task<string> SendRequestAsync(string methodParameters, string id, string url)
        {
            if (Account == null) throw new InvalidOperationException("Account has not been set to send the request");
            return GetResponseString(Account.Address, id);
        }

        public virtual string GetResponseString(string address, string id)
        {
            return $"currentRequestCallback[\"{id}\"](null, ['{address}'])";
        }

        public string GetSyncScrypt()
        {
            return $@"
               if (payload.method === 'eth_accounts') {{
                    return respondSync(null,['{Account.Address}']);
               }}";
        }
    }
}
