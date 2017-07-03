# Nethereum.DappHybrid

The Nethereum DappHybrid is a cross platform hybrid hosting mechanism for web based decentralised applications. Currently it has been tested on UWP (Windows Desktop), Android and iOS. Samples for Mac, Linux and WPF will be available as they appear or come out of beta on Xamarin.Forms.

## Why?

There are times when you don't have time or there is not business benefit to create a cross-platform native application (i.e using Xamarin.Forms, or React Native), but you still want to provide a native applications (either on mobile or desktop) in which they can load and / or manage the ethereum accounts.

## How does it work ?

Similarly to  Metamask, a provider has been created that is injected on a web host, allowing the interception of any RPC method. 

This way your application can control and manage the accounts and offline transaction signing.

## Request sample

```csharp
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


```




