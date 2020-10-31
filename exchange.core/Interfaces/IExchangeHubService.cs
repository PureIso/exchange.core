using System.Collections.Generic;
using System.Threading.Tasks;
using exchange.core.models;

namespace exchange.core.interfaces
{
    public interface IExchangeHubService
    {
        Task RequestedAccountInfo();
        Task RequestedCurrentPrices();
        Task RequestedProducts();
        Task RequestedApplications();
        Task RequestedSubscription(string applicationName, List<string> symbols);
        Task NotifyMainCurrency(string applicationName, string mainCurrency);
        Task NotifyApplications(List<string> applicationNames);
        Task NotifyAssetInformation(string applicationName, Dictionary<string, AssetInformation> assetInformation);
        Task NotifyCurrentPrices(string applicationName, Dictionary<string, decimal> currentPrices);
        Task NotifyAccountInfo(string applicationName, Dictionary<string, decimal> accountInformation);
        Task NotifyProductChange(string applicationName, List<Product> products);
    }
}