using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace exchange.signalr.client
{
    public partial class MainForm : Form
    {
        private HubConnection _connection;
        public MainForm()
        {
            InitializeComponent();
            string hubAddress = "https://localhost:5001/hubs/exchange";
            _connection = new HubConnectionBuilder().WithUrl(hubAddress).Build(); // WithUrl not found
            //_connection.ConnectionId
            //ou need a SignalR client. You can’t send posts from a normal web client
            _connection.On<Dictionary<string,decimal>>("CurrentPrices", currentPrices =>
            {
                List<ListViewItem> listViewItems = new List<ListViewItem>();
                Console.WriteLine($"================Current Prices======================");
                foreach(KeyValuePair<string, decimal> item in currentPrices)
                {
                    listViewItems.Add(new ListViewItem(new string []{ item.Key, item.Value.ToString() }));
                }
                currentPriceListView.UpdateListViewItemsThreadSafe(listViewItems.ToArray());
            });

            _connection.StartAsync();

            _connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _connection.StartAsync();
            };
        }

    }
}
