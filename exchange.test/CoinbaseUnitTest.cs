using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase;
using exchange.core.Enums;
using exchange.core.helpers;
using exchange.core.implementations;
using exchange.core.models;
using exchange.core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseUnitTest
    {
        [TestInitialize]
        public void Initialize()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _connectionAdapter = new ConnectionAdapter
            {
                Authentication = new Authentication("api_key", "passphrase", "NiWaGaqmhB3lgI/tQmm/gQ==",
                    "https://api.pro.coinbase.com", "wss://ws-feed.gdax.com")
            };
        }

        #region Fields

        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private ConnectionAdapter _connectionAdapter;

        #endregion

        #region HTTP Client Test

        [TestMethod]
        public void UpdateAccounts_ShouldReturnAccounts_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[{
                            ""id"": ""71452118-efc7-4cc4-8780-a5e22d4baa53"",
                            ""currency"": ""BTC"",
                            ""balance"": ""0.0000000000000000"",
                            ""available"": ""0.0000000000000000"",
                            ""hold"": ""0.0000000000000000"",
                            ""profile_id"": ""75da88c5-05bf-4f54-bc85-5c775bd68254"",
                            ""trading_enabled"":true
                            },
                            {
                            ""id"": ""e316cb9a-0808-4fd7-8914-97829c1925de"",
                            ""currency"": ""USD"",
                            ""balance"": ""80.2301373066930000"",
                            ""available"": ""79.2266348066930000"",
                            ""hold"": ""1.0035025000000000"",
                            ""profile_id"": ""75da88c5-05bf-4f54-bc85-5c775bd68254""
                            }]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            //Act
            subjectUnderTest.UpdateAccountsAsync().Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Accounts);
            Assert.AreEqual(2, subjectUnderTest.Accounts.Count);

            Assert.AreEqual("71452118-efc7-4cc4-8780-a5e22d4baa53", subjectUnderTest.Accounts[0].ID);
            Assert.AreEqual("BTC", subjectUnderTest.Accounts[0].Currency);
            Assert.AreEqual((decimal) 0.0000000000000000, subjectUnderTest.Accounts[0].Balance.ToDecimal());
            Assert.AreEqual((decimal) 0.0000000000000000, subjectUnderTest.Accounts[0].Available.ToDecimal());
            Assert.AreEqual((decimal) 0.0000000000000000, subjectUnderTest.Accounts[0].Hold.ToDecimal());

            Assert.AreEqual("e316cb9a-0808-4fd7-8914-97829c1925de", subjectUnderTest.Accounts[1].ID);
            Assert.AreEqual("USD", subjectUnderTest.Accounts[1].Currency);
            Assert.AreEqual((decimal) 80.23013731, subjectUnderTest.Accounts[1].Balance.ToDecimal());
            Assert.AreEqual((decimal) 79.22663481, subjectUnderTest.Accounts[1].Available.ToDecimal());
            Assert.AreEqual((decimal) 1.00350250, subjectUnderTest.Accounts[1].Hold.ToDecimal());
        }

        [TestMethod]
        public void UpdateAccountsWithParameter_ShouldReturnAccounts_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[{
                            ""id"": ""71452118-efc7-4cc4-8780-a5e22d4baa53"",
                            ""currency"": ""BTC"",
                            ""balance"": ""0.0000000000000000"",
                            ""available"": ""0.0000000000000000"",
                            ""hold"": ""0.0000000000000000"",
                            ""profile_id"": ""75da88c5-05bf-4f54-bc85-5c775bd68254"",
                            ""trading_enabled"":true
                            }]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            //Act
            subjectUnderTest.UpdateAccountsAsync("71452118-efc7-4cc4-8780-a5e22d4baa53").Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Accounts);
            Assert.AreEqual(1, subjectUnderTest.Accounts.Count);

            Assert.AreEqual("71452118-efc7-4cc4-8780-a5e22d4baa53", subjectUnderTest.Accounts[0].ID);
            Assert.AreEqual("BTC", subjectUnderTest.Accounts[0].Currency);
            Assert.AreEqual((decimal) 0.0000000000000000, subjectUnderTest.Accounts[0].Balance.ToDecimal());
            Assert.AreEqual((decimal) 0.0000000000000000, subjectUnderTest.Accounts[0].Available.ToDecimal());
            Assert.AreEqual((decimal) 0.0000000000000000, subjectUnderTest.Accounts[0].Hold.ToDecimal());
        }

        [TestMethod]
        public void UpdateAccountHistory_ShouldReturnAccountHistory_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[
                            {
                                ""id"": ""100"",
                                ""created_at"": ""2014-11-07T08:19:27.028459Z"",
                                ""amount"": ""0.001"",
                                ""balance"": ""239.669"",
                                ""type"": ""fee"",
                                ""details"": {
                                    ""order_id"": ""d50ec984-77a8-460a-b958-66f114b0de9b"",
                                    ""trade_id"": ""74"",
                                    ""product_id"": ""BTC-USD""
                                }
                            }
                        ]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            //Act
            subjectUnderTest.UpdateAccountHistoryAsync("100").Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.AccountHistories);
            Assert.AreEqual(1, subjectUnderTest.AccountHistories.Count);
            Assert.AreEqual("100", subjectUnderTest.AccountHistories[0].ID);
            Assert.IsNotNull(subjectUnderTest.AccountHistories[0].Detail);
            Assert.AreEqual("d50ec984-77a8-460a-b958-66f114b0de9b",
                subjectUnderTest.AccountHistories[0].Detail.OrderID);
        }

        [TestMethod]
        public void UpdateAccountHolds_ShouldReturnAccountHolds_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[
                            {
                                ""id"": ""82dcd140-c3c7-4507-8de4-2c529cd1a28f"",
                                ""account_id"": ""e0b3f39a-183d-453e-b754-0c13e5bab0b3"",
                                ""created_at"": ""2014-11-06T10:34:47.123456Z"",
                                ""updated_at"": ""2014-11-06T10:40:47.123456Z"",
                                ""amount"": ""4.23"",
                                ""type"": ""order"",
                                ""ref"": ""0a205de4-dd35-4370-a285-fe8fc375a273""
                            }
                           ]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            //Act
            subjectUnderTest.UpdateAccountHoldsAsync("82dcd140-c3c7-4507-8de4-2c529cd1a28f").Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.AccountHolds);
            Assert.AreEqual(1, subjectUnderTest.AccountHolds.Count);
            Assert.AreEqual("82dcd140-c3c7-4507-8de4-2c529cd1a28f", subjectUnderTest.AccountHolds[0].ID);
            Assert.AreEqual("e0b3f39a-183d-453e-b754-0c13e5bab0b3", subjectUnderTest.AccountHolds[0].AccountID);
        }

        [TestMethod]
        public void UpdateOrders_ShouldReturnAllOrders_WhenOrdersExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[
                            {
                                ""id"": ""d0c5340b-6d6c-49d9-b567-48c4bfca13d2"",
                                ""price"": ""0.10000000"",
                                ""size"": ""0.01000000"",
                                ""product_id"": ""BTC-USD"",
                                ""side"": ""buy"",
                                ""stp"": ""dc"",
                                ""type"": ""limit"",
                                ""time_in_force"": ""GTC"",
                                ""post_only"": false,
                                ""created_at"": ""2016-12-08T20:02:28.53864Z"",
                                ""fill_fees"": ""0.0000000000000000"",
                                ""filled_size"": ""0.00000000"",
                                ""executed_value"": ""0.0000000000000000"",
                                ""status"": ""open"",
                                ""settled"": false
                            },
                            {
                                ""id"": ""8b99b139-58f2-4ab2-8e7a-c11c846e3022"",
                                ""price"": ""1.00000000"",
                                ""size"": ""1.00000000"",
                                ""product_id"": ""BTC-USD"",
                                ""side"": ""buy"",
                                ""stp"": ""dc"",
                                ""type"": ""limit"",
                                ""time_in_force"": ""GTC"",
                                ""post_only"": false,
                                ""created_at"": ""2016-12-08T20:01:19.038644Z"",
                                ""fill_fees"": ""0.0000000000000000"",
                                ""filled_size"": ""0.00000000"",
                                ""executed_value"": ""0.0000000000000000"",
                                ""status"": ""open"",
                                ""settled"": false
                            }
                        ]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            //Act
            subjectUnderTest.UpdateOrdersAsync().Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Orders);
            Assert.AreEqual(2, subjectUnderTest.Orders.Count);
            Assert.AreEqual("d0c5340b-6d6c-49d9-b567-48c4bfca13d2", subjectUnderTest.Orders[0].ID);
            Assert.AreEqual("8b99b139-58f2-4ab2-8e7a-c11c846e3022", subjectUnderTest.Orders[1].ID);
        }

        [TestMethod]
        public void PostOrders_ShouldReturnPostedOrder_WhenOrderIsSuccessful()
        {
            //Arrange
            Order order = new Order
                {Size = "0.01", Price = "0.100", Side = OrderSide.Buy.GetStringValue(), ProductID = "BTC-EUR"};
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"{
                            ""id"": ""d0c5340b-6d6c-49d9-b567-48c4bfca13d2"",
                            ""price"": ""0.10000000"",
                            ""size"": ""0.01000000"",
                            ""product_id"": ""BTC-EUR"",
                            ""side"": ""buy"",
                            ""stp"": ""dc"",
                            ""type"": ""limit"",
                            ""time_in_force"": ""GTC"",
                            ""post_only"": false,
                            ""created_at"": ""2016-12-08T20:02:28.53864Z"",
                            ""fill_fees"": ""0.0000000000000000"",
                            ""filled_size"": ""0.00000000"",
                            ""executed_value"": ""0.0000000000000000"",
                            ""status"": ""pending"",
                            ""settled"": false
                           }")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            //Act
            Order orderResult = subjectUnderTest.PostOrdersAsync(order).Result;
            //Assert
            Assert.IsNotNull(orderResult);
            Assert.AreEqual((decimal) 0.10000000, orderResult.Price.ToDecimal());
            Assert.AreEqual("BTC-EUR", orderResult.ProductID);
        }

        [TestMethod]
        public void CancelOrders_ShouldReturnCancelledOrders_WhenOrdersExists()
        {
            //Arrange
            Product product = new Product {ID = "BTC-EUR"};
            List<Order> orders = new List<Order>
            {
                new Order {ID = "71ad6d95-c70b-49a5-871d-d5dcc3295c45", ProductID = "BTC-EUR"},
                new Order {ID = "00000000-0000-0000-0000-000000000000", ProductID = "BTC-USD"}
            };
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[""71ad6d95-c70b-49a5-871d-d5dcc3295c45""]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            subjectUnderTest.Orders = orders;
            //Act
            List<Order> removedOrders = subjectUnderTest.CancelOrdersAsync(product).Result;
            //Assert
            Assert.IsNotNull(removedOrders);
            Assert.AreEqual(1, removedOrders.Count);
            Assert.AreEqual(1, subjectUnderTest.Orders.Count);
        }

        [TestMethod]
        public void CancelOrders_ShouldReturnCancelledOrders_WhenOrdersExistsAndItsNonArray()
        {
            //Arrange
            Product product = new Product {ID = "BTC-EUR"};
            List<Order> orders = new List<Order>
            {
                new Order {ID = "71ad6d95-c70b-49a5-871d-d5dcc3295c45", ProductID = "BTC-EUR"},
                new Order {ID = "00000000-0000-0000-0000-000000000000", ProductID = "BTC-USD"}
            };
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"""71ad6d95-c70b-49a5-871d-d5dcc3295c45""")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            subjectUnderTest.Orders = orders;
            //Act
            List<Order> removedOrders = subjectUnderTest.CancelOrdersAsync(product).Result;
            //Assert
            Assert.IsNotNull(removedOrders);
            Assert.AreEqual(1, removedOrders.Count);
            Assert.AreEqual(1, subjectUnderTest.Orders.Count);
        }

        [TestMethod]
        public void CancelOrder_ShouldReturnCancelledOrders_WhenOrdersExists()
        {
            //Arrange
            Order order = new Order {ID = "71ad6d95-c70b-49a5-871d-d5dcc3295c45", ProductID = "BTC-EUR"};
            List<Order> orders = new List<Order>
            {
                new Order {ID = "71ad6d95-c70b-49a5-871d-d5dcc3295c45", ProductID = "BTC-EUR"},
                new Order {ID = "00000000-0000-0000-0000-000000000000", ProductID = "BTC-USD"}
            };
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[""71ad6d95-c70b-49a5-871d-d5dcc3295c45""]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            subjectUnderTest.Orders = orders;
            //Act
            List<Order> removedOrders = subjectUnderTest.CancelOrderAsync(order).Result;
            //Assert
            Assert.IsNotNull(removedOrders);
            Assert.AreEqual(1, removedOrders.Count);
            Assert.AreEqual(1, subjectUnderTest.Orders.Count);
        }

        [TestMethod]
        public void CancelOrder_ShouldReturnCancelledOrders_WhenOrdersExistsAndItsNonArray()
        {
            //Arrange
            Order order = new Order {ID = "71ad6d95-c70b-49a5-871d-d5dcc3295c45", ProductID = "BTC-EUR"};
            List<Order> orders = new List<Order>
            {
                new Order {ID = "71ad6d95-c70b-49a5-871d-d5dcc3295c45", ProductID = "BTC-EUR"},
                new Order {ID = "00000000-0000-0000-0000-000000000000", ProductID = "BTC-USD"}
            };
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"""71ad6d95-c70b-49a5-871d-d5dcc3295c45""")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            subjectUnderTest.Orders = orders;
            //Act
            List<Order> removedOrders = subjectUnderTest.CancelOrderAsync(order).Result;
            //Assert
            Assert.IsNotNull(removedOrders);
            Assert.AreEqual(1, removedOrders.Count);
            Assert.AreEqual(1, subjectUnderTest.Orders.Count);
        }

        [TestMethod]
        public void UpdateProducts_ShouldReturnProducts_WhenProductExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[{
                            ""id"": ""BTC-EUR"",
                            ""base_currency"": ""BTC"",
                            ""quote_currency"": ""EUR"",
                            ""base_min_size"": ""0.001"",
                            ""base_max_size"": ""10000.00"",
                            ""quote_increment"": ""0.01""
                            },
                            {
                            ""id"": ""ETH-EUR"",
                            ""base_currency"": ""ETH"",
                            ""quote_currency"": ""EUR"",
                            ""base_min_size"": ""0.001"",
                            ""base_max_size"": ""10000.00"",
                            ""quote_increment"": ""0.01""
                            }]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            //Act
            subjectUnderTest.UpdateProductsAsync().Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Products);
            Assert.AreEqual(2, subjectUnderTest.Products.Count);
            Assert.AreEqual("BTC-EUR", subjectUnderTest.Products[0].ID);
        }

        [TestMethod]
        public void UpdateTickers_ShouldReturnProductsAndTickers_WhenProductsAndTickersExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"{""price"": ""333.99"",""trade_id"": 4729088,""size"": ""0.193"",""bid"": ""333.98"",""ask"": ""333.99"",""volume"": ""5957.11914015"",""time"": ""2015-11-14T20:46:03.511254Z""}")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            List<Product> products = new List<Product>
            {
                new Product
                {
                    ID = "BTC-EUR",
                    BaseCurrency = "BTC",
                    QuoteCurrency = "EUR",
                    BaseMinSize = "0.001",
                    BaseMaxSize = "10000.00",
                    QuoteIncrement = "0.01"
                }
            };
            //Act
            subjectUnderTest.UpdateTickersAsync(products).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Tickers);
            Assert.AreEqual(1, subjectUnderTest.Tickers.Count);
            Assert.AreEqual("BTC-EUR", subjectUnderTest.Tickers[0].ProductID);
            Assert.AreEqual(4729088, subjectUnderTest.Tickers[0].TradeID);
            Assert.AreEqual((decimal) 333.99, subjectUnderTest.Tickers[0].Price.ToDecimal());
            Assert.AreEqual((decimal) 0.193, subjectUnderTest.Tickers[0].Size.ToDecimal());
            Assert.AreEqual((decimal) 333.98, subjectUnderTest.Tickers[0].Bid.ToDecimal());
            Assert.AreEqual((decimal) 333.99, subjectUnderTest.Tickers[0].Ask.ToDecimal());
            Assert.AreEqual((decimal) 5957.11914015, subjectUnderTest.Tickers[0].Volume.ToDecimal());
            Assert.AreEqual("2015-11-14T20:46:03.511254Z".ToDateTime(), subjectUnderTest.Tickers[0].Time.ToDateTime());
            Assert.IsNotNull(subjectUnderTest.CurrentPrices);
            Assert.AreEqual(subjectUnderTest.CurrentPrices[subjectUnderTest.Tickers[0].ProductID],
                subjectUnderTest.Tickers[0].Price.ToDecimal());
        }

        [TestMethod]
        public void UpdateFills_ShouldReturnFills_WhenFillsExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[{
                            ""trade_id"": 74,
                            ""product_id"": ""BTC-EUR"",
                            ""price"": ""10.00"",
                            ""size"": ""0.01"",
                            ""order_id"": ""d50ec984-77a8-460a-b958-66f114b0de9b"",
                            ""created_at"": ""2014-11-07T22:19:28.578544Z"",
                            ""liquidity"": ""T"",
                            ""fee"": ""0.00025"",
                            ""settled"": true,
                            ""side"": ""buy""
                            },
                            {
                            ""trade_id"": 75,
                            ""product_id"": ""BTC-EUR"",
                            ""price"": ""10.00"",
                            ""size"": ""0.01"",
                            ""order_id"": ""990ec984-77a8-460a-b958-66f114b0de9c"",
                            ""created_at"": ""2014-12-07T22:19:28.578544Z"",
                            ""liquidity"": ""T"",
                            ""fee"": ""0.00025"",
                            ""settled"": true,
                            ""side"": ""sell""
                            }]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            Product product = new Product
            {
                ID = "BTC-EUR",
                BaseCurrency = "BTC",
                QuoteCurrency = "EUR",
                BaseMinSize = "0.001",
                BaseMaxSize = "10000.00",
                QuoteIncrement = "0.01"
            };

            //Act
            subjectUnderTest.UpdateFillsAsync(product).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Fills);
            Assert.AreEqual(2, subjectUnderTest.Fills.Count);
            Assert.AreEqual(product.ID, subjectUnderTest.Fills[0].ProductID);
        }

        [TestMethod]
        public void UpdateProductOrderBook_ShouldReturnOrderBook_WhenProductExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"{
                            ""sequence"": 3,
                            ""bids"": [[""152.02"",""0.0509"",1],[""151.9"",""1.6256"",1],[""151.89"",""12.8585"",1]],
                            ""asks"": [[""152.37"", ""5"", 1],[""152.38"",""12.8561"",1]]
                            }")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            Product product = new Product
            {
                ID = "BTC-EUR",
                BaseCurrency = "BTC",
                QuoteCurrency = "EUR",
                BaseMinSize = "0.001",
                BaseMaxSize = "10000.00",
                QuoteIncrement = "0.01"
            };

            //Act
            subjectUnderTest.UpdateProductOrderBookAsync(product).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.OrderBook);
            Assert.AreEqual(3, subjectUnderTest.OrderBook.Bids.ToOrderList().Count);
            Assert.AreEqual(2, subjectUnderTest.OrderBook.Asks.ToOrderList().Count);
            Assert.AreEqual((decimal) 152.02, subjectUnderTest.OrderBook.Bids.ToOrderList()[0].Price.ToDecimal());
            Assert.AreEqual("5", subjectUnderTest.OrderBook.Asks.ToOrderList()[0].Size);
        }

        [TestMethod]
        public void UpdateProductHistoricCandles_ShouldReturnOrderBook_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        @"[[1415398998, 0.33, 4.9, 0.40, 4.5, 15.3],[1415398768, 0.32, 4.2, 0.35, 4.2, 12.3]]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _connectionAdapter.HttpClient = httpClient;
            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ConnectionAdapter = _connectionAdapter;
            Product product = new Product
            {
                ID = "BTC-EUR",
                BaseCurrency = "BTC",
                QuoteCurrency = "EUR",
                BaseMinSize = "0.001",
                BaseMaxSize = "10000.00",
                QuoteIncrement = "0.01"
            };
            DateTime startingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
            DateTime endingDateTime = startingDateTime.AddMonths(6).ToUniversalTime();
            //Act
            HistoricCandlesSearch historicCandlesSearch = new HistoricCandlesSearch();
            historicCandlesSearch.Symbol = "BTC-EUR";
            historicCandlesSearch.StartingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
            historicCandlesSearch.EndingDateTime =
                historicCandlesSearch.StartingDateTime.AddMonths(6).ToUniversalTime();
            subjectUnderTest.UpdateProductHistoricCandlesAsync(historicCandlesSearch).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.HistoricRates);
            Assert.AreEqual(2, subjectUnderTest.HistoricRates.Count);
            Assert.AreEqual((decimal) 0.32, subjectUnderTest.HistoricRates[0].Low);
            Assert.AreEqual((decimal) 12.3, subjectUnderTest.HistoricRates[0].Volume);
        }

        #endregion

        #region Web Socket Test

        [TestMethod]
        public void WebSocketSubscribe_ShouldReturnTrue()
        {
            //Arrange
            List<Product> products = new List<Product>
            {
                new Product
                {
                    ID = "BTC-EUR",
                    BaseCurrency = "BTC",
                    QuoteCurrency = "EUR",
                    BaseMinSize = "0.001",
                    BaseMaxSize = "10000.00",
                    QuoteIncrement = "0.01"
                },
                new Product
                {
                    ID = "ETH-EUR",
                    BaseCurrency = "ETH",
                    QuoteCurrency = "EUR",
                    BaseMinSize = "0.001",
                    BaseMaxSize = "10000.00",
                    QuoteIncrement = "0.01"
                }
            };
            HttpClient httpClient = new HttpClient();
            ClientWebSocket clientWebSocket = new ClientWebSocket();
            Mock<ConnectionAdapter> connectionFactoryMock =
                new Mock<ConnectionAdapter>(MockBehavior.Strict, httpClient);
            connectionFactoryMock.Setup(x => x.WebSocketSendAsync(products.ToSubscribeString()))
                .ReturnsAsync(
                    @"{""type"":""subscriptions"",""channels"":[{""name"":""ticker"",""product_ids"":[""BTC-EUR"",""ETH-EUR""]}]}");
            connectionFactoryMock.Setup(x => x.IsWebSocketConnected()).Returns(true);
            connectionFactoryMock.Setup(x => x.WebSocketReceiveAsync())
                .Returns(Task.FromResult(
                    @"{""type"":""subscriptions"",""channels"":[{""name"":""ticker"",""product_ids"":[""BTC-EUR"",""ETH-EUR""]}]}"));

            Coinbase subjectUnderTest = new Coinbase();
            subjectUnderTest.ClientWebSocket = clientWebSocket;
            subjectUnderTest.ConnectionAdapter = connectionFactoryMock.Object;

            //Act
            subjectUnderTest.ChangeFeed(products);
            Assert.IsTrue(true);
        }

        //[TestMethod]
        //[Timeout(1000)]
        //public void WebSocketProcessFeed_ShouldReturnFeed()
        //{
        //    //Arrange
        //    HttpClient httpClient = new HttpClient();
        //    ClientWebSocket clientWebSocket = new ClientWebSocket();
        //    Mock<ConnectionAdapter> connectionFactoryMock =
        //        new Mock<ConnectionAdapter>(MockBehavior.Strict, httpClient);
        //    connectionFactoryMock.Object.Authentication = new Authentication("api_key", "passphrase",
        //        "NiWaGaqmhB3lgI/tQmm/gQ==", "https://api.pro.coinbase.com", "wss://ws-feed.gdax.com");
        //    connectionFactoryMock
        //        .Setup(x => x.ConnectAsync(connectionFactoryMock.Object.Authentication.WebSocketUri.ToString()))
        //        .Returns(Task.CompletedTask);
        //    connectionFactoryMock.Setup(x => x.IsWebSocketConnected()).Returns(true);
        //    connectionFactoryMock.SetupSequence(f => f.WebSocketReceiveAsync())
        //        .Returns(Task.FromResult(@"{
        //                                    ""type"":""ticker"",
        //                                    ""sequence"":7000000000,
        //                                    ""product_id"":""BTC-EUR"",
        //                                    ""price"":""6693.2"",
        //                                    ""open_24h"":""6785.59000000"",
        //                                    ""volume_24h"":""1778.78223836"",
        //                                    ""low_24h"":""6566.00000000"",
        //                                    ""high_24h"":""6813.00000000"",
        //                                    ""volume_30d"":""152160.22176000"",
        //                                    ""best_bid"":""6693.20"",
        //                                    ""best_ask"":""6698.12"",
        //                                    ""side"":""sell"",
        //                                    ""time"":""2020-04-09T23:09:28.709968Z"",
        //                                    ""trade_id"":25684401,
        //                                    ""last_size"":""0.0027496""
        //                                    }")) // will be returned on 1st invocation
        //        .Returns(Task.FromResult(@"{
        //                                    ""type"":""ticker"",
        //                                    ""sequence"":7000000001,
        //                                    ""product_id"":""BTC-EUR"",
        //                                    ""price"":""6700.34"",
        //                                    ""open_24h"":""6785.59000000"",
        //                                    ""volume_24h"":""1778.79785469"",
        //                                    ""low_24h"":""6566.00000000"",
        //                                    ""high_24h"":""6813.00000000"",
        //                                    ""volume_30d"":""152160.22176000"",
        //                                    ""best_bid"":""6695.20"",
        //                                    ""best_ask"":""6700.12"",
        //                                    ""side"":""buy"",
        //                                    ""time"":""2020-04-09T23:09:57.499045Z"",
        //                                    ""trade_id"":25684402,
        //                                    ""last_size"":""0.01428046""
        //                                    }")) // will be returned on 2nd invocation
        //        .Returns(Task.FromResult(@"{
        //                                    ""type"":""ticker"",
        //                                    ""sequence"":7000000002,
        //                                    ""product_id"":""BTC-EUR"",
        //                                    ""price"":""6695.64"",
        //                                    ""open_24h"":""6785.59000000"",
        //                                    ""volume_24h"":""1778.79785469"",
        //                                    ""low_24h"":""6566.00000000"",
        //                                    ""high_24h"":""6813.00000000"",
        //                                    ""volume_30d"":""152160.22176000"",
        //                                    ""best_bid"":""6695.64"",
        //                                    ""best_ask"":""6699.80"",
        //                                    ""side"":""sell"",
        //                                    ""time"":""2020-04-09T23:10:01.022034Z"",
        //                                    ""trade_id"":25684403,
        //                                    ""last_size"":""0.00133587""
        //                                    }")); // will be returned on 3rd invocation
        //    Coinbase subjectUnderTest = new Coinbase();
        //    subjectUnderTest.ConnectionAdapter = connectionFactoryMock.Object;
        //    bool eventRaised = false;
        //    int eventRaisedCount = 0;
        //    AutoResetEvent autoEvent = new AutoResetEvent(false);
        //    subjectUnderTest.ClientWebSocket = clientWebSocket;
        //    subjectUnderTest.FeedBroadcast += delegate(string applicationame, Feed feed)
        //    {
        //        eventRaised = true;
        //        if (feed.Sequence == 7000000002 || feed.Sequence == 7000000001 || feed.Sequence == 7000000000)
        //            eventRaisedCount++;
        //        if (eventRaisedCount >= 3)
        //            autoEvent.Set();
        //    };
        //    //Act
        //    //subjectUnderTest.StartProcessingFeed();
        //    autoEvent.WaitOne();
        //    Assert.IsTrue(eventRaised);
        //}

        #endregion
    }
}