using Nethereum.Web3.Accounts;

namespace Nethereum.DappHybrid
{
    public interface IRpcRequestRequiredAccount
    {
        IAccount Account { get; set; }
    }
}