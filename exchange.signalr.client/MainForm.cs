using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using exchange.core.Enums;
using exchange.core.interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace exchange.signalR.client.win.form
{
    public partial class MainForm : Form , IExchangeHub
    {
        public Task NotifyCurrentPrices(Dictionary<string, decimal> currentPrices)
        {
            return Task.Run(() =>
            {
                List<ListViewItem> listViewItems = new List<ListViewItem>();
                foreach ((string key, decimal value) in currentPrices)
                {
                    listViewItems.Add(new ListViewItem(new string[] { key, value.ToString(CultureInfo.InvariantCulture) }));
                }
                currentPriceListView.UpdateListViewItemsThreadSafe(listViewItems.ToArray());
            });
        }

        public Task NotifyInformation(MessageType messageType, string message)
        {
            return Task.Run(() =>
            {
                logRichTextBox.LogTextEvent(messageType, message);
            });
        }

        public MainForm()
        {
            InitializeComponent();
            const string hubAddress = "https://localhost:5001/hubs/exchange";
            HubConnection connection = new HubConnectionBuilder().WithUrl(hubAddress).Build();
            //you need a SignalR client. You can’t send posts from a normal web client
            connection.On<Dictionary<string,decimal>>(nameof(IExchangeHub.NotifyCurrentPrices), NotifyCurrentPrices);
            connection.On<MessageType,string>(nameof(IExchangeHub.NotifyInformation), NotifyInformation);
            connection.StartAsync();

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };
        }

    }
}
