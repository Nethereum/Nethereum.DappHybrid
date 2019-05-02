# Nethereum.DappHybrid

The Nethereum DappHybrid is a cross platform hybrid hosting mechanism for web based decentralised applications. Currently it has been tested on UWP (Windows Desktop), Android and iOS. Samples for Mac, Linux and WPF will be available as they appear or come out of beta on Xamarin.Forms.

## Why?
You have created an html / javascript dapp, and you want to provide a cross platform "native" desktop / mobile application reusing your existing dapp (as opposed to creating a new Xamarin.Forms implementation as an example), but still want to have the capability for your application to manage the rpc connections and manage the accounts internally (i.e sigining trasactions)

## How does it work ?
Using a webview to host the application, and similarly to  Metamask, a web3.js provider has been created that is injected on a web host, allowing the rpc connection configuration and the interception of any RPC method, which we want to manage internally.

### The WebView Host
First of all we need a WebView to host the html / javascript dapp.  The sample uses the Xamarin.Forms plugin "Xam.Plugin.WebView", this is a great plugin with cross platform JavaScript invocation and injection support, reducing the need of creating and managing our own HybridView.

```xml
 <fwv:FormsWebView x:Name="webHybridDapp" ContentType="Internet"
                          BackgroundColor="Teal" Grid.Row="0" Grid.Column="0" />
```

### Injecting Web3js and our Custom Provider

When our dapp page has been loaded, we will then inject our Web3js, our custom Nethereum Provider, web3 initialisation and finally retrigger the "load" event to ensure our page is intialised with the injected web3 and provider.

```csharp
webHybridDapp.OnContentLoaded += (cobj) =>
            {
                //Inject web3 script bundle
                webHybridDapp.InjectJavascript(dappHybridViewModel.ProviderService.GetWeb3Script());
                webHybridDapp.InjectJavascript(dappHybridViewModel.ProviderService.GetNethereumEmbeddedProviderScript());
                webHybridDapp.InjectJavascript(dappHybridViewModel.ProviderService.GetWeb3InitScript());
                //trigger the event load, as the usual pattern is to startup dapps on load with web3 injected
                webHybridDapp.InjectJavascript("dispatchEvent(new Event('load'));");
            };
```

Our dapp will need to have the same intialiasation checks as per Metamask, so when loaded our custom provider will be injected.

```
window.addEventListener('load', function() {

  // Checking if Web3 has been injected by the host
  if (typeof web3 !== 'undefined') {
    // Use the host provider
    window.web3 = new Web3(web3.currentProvider);
  } else {
    //default settings
    window.web3 = new Web3(new Web3.providers.HttpProvider("http://localhost:8545"));
  }
  // Now you can start your app & access web3 freely:
  startApp()

})
```

Finally we need to register the callback method from our provider to the C# Host, this generic method is the one responsible to accept all the calls from the dapp into our .net host.

```csharp
 webHybridDapp.RegisterLocalCallback(Nethereum.DappHybrid.Scripts.CallToDappHostFunctionName, DappCallBack);
```

The callback data will be then pass to our ProviderService

```csharp
public async void DappCallBack(string data)
        {
            try
            {
                var response = await dappHybridViewModel.ProviderService.SendRequestAsync(data);
                Device.BeginInvokeOnMainThread(() =>
                {
                    webHybridDapp.InjectJavascript(response);

                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                await DisplayAlert("Error", ex.Message, "Ok");
            }
        }
```

### The provider service
The provider service manages the registration of the requests to intercept, the building of our custom provider and web3 intialisation and the routing of callbacks to the requests registered.

For example on our ViewModel we can initialise our Provider with the url of the rpc client like "http://mainnet.infura.io:8545", and our Account initialised with its private key from key store, or if it is a ManagedAccount with the unlocking password.

```csharp
 ProviderService = new ProviderRequestService(Settings.RpcUrl, Settings.Account);
 ProviderService.RegisterProviderRequest(new EthAccountsProviderRequest());
 EthSendTransactionProviderRequest = new EthSendTransactionProviderRequest();
 ProviderService.RegisterProviderRequest(EthSendTransactionProviderRequest);
```

### Request sample
Signing transaction is probably the most common use case for intercepting a request. 

To implement this custom request we need to provide the javascript async implementation, sync implementation and the SendRequest implementation in C#.

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


Note: in this sample, a special INFURA API key is used: `7238211010344719ad14a89db874158c`. If you wish to use this sample in your own project you’ll need to [sign up on INFURA](https://infura.io/register) and use your own key.
