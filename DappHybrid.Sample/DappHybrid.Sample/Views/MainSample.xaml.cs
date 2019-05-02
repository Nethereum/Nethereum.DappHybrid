using Nethereum.DappHybrid;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace DappHybrid.Sample.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainSample : TabbedPage
    {
        DappHybridViewModel dappHybridViewModel;
        string SourceUrl = "http://flappyeth.nethereum.com/";

        public MainSample()
        {
            InitializeComponent();
            webHybridDapp.OnNavigationError += WebHybridDapp_OnNavigationError;
            dappHybridViewModel = new DappHybridViewModel();
            this.BindingContext = dappHybridViewModel;
            
            ConfigureHybridDappScripts();
           
        }

        private void WebHybridDapp_OnNavigationError(Xam.Plugin.Abstractions.Events.Inbound.NavigationErrorDelegate eventObj)
        {
            Debug.WriteLine(eventObj.ErrorCode);
        }

        //injected scripts to dapp
        void ConfigureHybridDappScripts()
        {
            webHybridDapp.OnContentLoaded += (cobj) =>
            {
                //Inject web3 script bundle
                webHybridDapp.InjectJavascript(dappHybridViewModel.ProviderService.GetWeb3Script());
                webHybridDapp.InjectJavascript(dappHybridViewModel.ProviderService.GetNethereumEmbeddedProviderScript());
                webHybridDapp.InjectJavascript(dappHybridViewModel.ProviderService.GetWeb3InitScript());
                //trigger the event load, as the usual pattern is to startup dapps on load with web3 injected
                webHybridDapp.InjectJavascript("dispatchEvent(new Event('load'));");
            };

            webHybridDapp.RegisterLocalCallback(Nethereum.DappHybrid.Scripts.CallToDappHostFunctionName, DappCallBack);
        }

        
        private void InitialiseProviderServiceAndReloadDappUrl()
        {
            if(dappHybridViewModel.Settings.Account != null)
            {
                dappHybridViewModel.InitialiseProviderService();
                dappHybridViewModel.EthSendTransactionProviderRequest.ValidateRequest = ValidateTransactionInput;
                //reload url
                webHybridDapp.Source = SourceUrl;
            }
        }

        //Confirmation to validate a trasaction, this is called by the TransactionProviderRequest
        public async Task<bool> ValidateTransactionInput(TransactionInput transactionInput)
        {
            return await DisplayAlert("Transaction confirmation", "Do you want to send a transaction to " + transactionInput.To, "Yes", "No");
        }

        //Call back from javascript to .net, to execute the rpc request
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

        private async void Button_Clicked(object sender, EventArgs e)
        {
            dappHybridViewModel.Settings.LoadAccount();
            if(dappHybridViewModel.Settings.Account != null)
            {
                InitialiseProviderServiceAndReloadDappUrl();
                //CurrentPage = Children[0];
            }
            else
            {
                await DisplayAlert("Error", "Invalid account input, check your private key", "Ok");
            }
            
        }
    }



    public class DappHybridViewModel
    {
        public SettingsViewModel Settings { get; set; }
        public IProviderRequestService ProviderService { get; private set; }
        public EthSendTransactionProviderRequest EthSendTransactionProviderRequest { get; private set; }

        public DappHybridViewModel()
        {
            Settings = new SettingsViewModel();
        }

        public void InitialiseProviderService()
        {
            ProviderService = new ProviderRequestService(Settings.RpcUrl, Settings.Account);
            ProviderService.RegisterProviderRequest(new EthAccountsProviderRequest());
            EthSendTransactionProviderRequest = new EthSendTransactionProviderRequest();
            ProviderService.RegisterProviderRequest(EthSendTransactionProviderRequest);
        }
    }

    public class SettingsViewModel
    {

        public SettingsViewModel()
        {
            this.RpcUrl = "https://
goerli.infura.io/v3/7238211010344719ad14a89db874158c
";
            this.PrivateKey = "0xb5b1870957d373ef0eeffecc6e4812c0fd08f554b37b233526acc331bf1544f7";
        }

        public IAccount Account { get; set; }

        public string Address {
            get
            {
                if (Account == null) return null;
                return Account.Address;
            }
        }

        public string PrivateKey { get; set; }

        public string RpcUrl { get; set; }

        public void LoadAccount()
        {
            if(PrivateKey != null)
            {
                try
                {
                    Account = new Account(PrivateKey);
                }
                catch
                {
                    Account = null;
                }
            }
        }
    }
}
