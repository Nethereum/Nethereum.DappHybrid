using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Nethereum.DappHybrid
{
    public class EthSendTransactionProviderRequest : IRpcProviderRequest, IRpcRequestRequiredAccount
    {
        public string GetMethodName()
        {
            return "eth_sendTransaction";
        }

        public virtual string GetAsyncScript()
        {
            return @"
            if (payload.method === 'eth_sendTransaction') {
                handledAsync = true;
                var id = Math.random();
                currentRequestCallback[id] = respond;
                //send transaction has only one parameter
                sendRequestToDotNetHost('eth_sendTransaction|' + id + '|' + JSON.stringify(arguments['0'].params[0]));
            }";
        }

        public virtual bool SyncSupported => false;

        public IAccount Account { get; set; }

        public Func<TransactionInput, Task<bool>> ValidateRequest { get; set; }

        public virtual async Task<string> SendRequestAsync(string methodParameters, string id, string url)
        {
            if (Account == null) throw new InvalidOperationException("Account has not been set to send the request");
            var transactionInput = JsonConvert.DeserializeObject<TransactionInput>(methodParameters);
            var validRequest = true;
            if (ValidateRequest != null) validRequest = await ValidateRequest(transactionInput);

            if (validRequest)
            {
                var web3 = new Nethereum.Web3.Web3(Account, url);
                var txn = await web3.TransactionManager.SendTransactionAsync(transactionInput);
                return GetResponseString(txn, id);
            }
            else
            {
                return Scripts.GetResponseError("Rejected transaction");
            }
        }

        public virtual string GetResponseString(string transaction, string id)
        {
            return $"currentRequestCallback[\"{id}\"](null, '{transaction}')";
        }

        public string GetSyncScrypt()
        {
            throw new NotImplementedException();
        }
    }
}
