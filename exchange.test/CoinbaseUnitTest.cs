using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using exchange.coinbase;
using exchange.coinbase.models;
using exchange.service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace exchange.test
{
    [TestClass]
    public class CoinbaseUnitTest
    {
        private ExchangeSettings _exchangeSettings;
        private Authentication _authentication;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;

        [TestInitialize()]
        public void Initialize() 
        {
            _exchangeSettings = new ExchangeSettings
            {
                APIKey = "api_key",
                PassPhrase = "pass_phrase",
                Secret = "NiWaGaqmhB3lgI/tQmm/gQ==",
                EndpointUrl = "https://api.pro.coinbase.com",
                Uri = "wss://ws-feed.gdax.com"
            };
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _authentication = new Authentication(_exchangeSettings.APIKey, _exchangeSettings.PassPhrase, _exchangeSettings.Secret, _exchangeSettings.EndpointUrl, _exchangeSettings.Uri);
        }

        [TestMethod]
        public void UpdateAccounts_ShouldReturnAccounts_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"[{{
                            ""id"": ""71452118-efc7-4cc4-8780-a5e22d4baa53"",
                            ""currency"": ""BTC"",
                            ""balance"": ""0.0000000000000000"",
                            ""available"": ""0.0000000000000000"",
                            ""hold"": ""0.0000000000000000"",
                            ""profile_id"": ""75da88c5-05bf-4f54-bc85-5c775bd68254"",
                            ""trading_enabled"":true
                            }},
                            {{
                            ""id"": ""e316cb9a-0808-4fd7-8914-97829c1925de"",
                            ""currency"": ""USD"",
                            ""balance"": ""80.2301373066930000"",
                            ""available"": ""79.2266348066930000"",
                            ""hold"": ""1.0035025000000000"",
                            ""profile_id"": ""75da88c5-05bf-4f54-bc85-5c775bd68254""
                            }}]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            Coinbase subjectUnderTest = new Coinbase(_authentication, httpClient);

            //Act
            subjectUnderTest.UpdateAccountsAsync().Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Accounts);
            Assert.AreEqual(2, subjectUnderTest.Accounts.Count);

            Assert.AreEqual("71452118-efc7-4cc4-8780-a5e22d4baa53", subjectUnderTest.Accounts[0].ID);
            Assert.AreEqual("BTC", subjectUnderTest.Accounts[0].Currency);
            Assert.AreEqual((decimal)0.0000000000000000, subjectUnderTest.Accounts[0].Balance);
            Assert.AreEqual((decimal)0.0000000000000000, subjectUnderTest.Accounts[0].Available);
            Assert.AreEqual((decimal)0.0000000000000000, subjectUnderTest.Accounts[0].Hold);

            Assert.AreEqual("e316cb9a-0808-4fd7-8914-97829c1925de", subjectUnderTest.Accounts[1].ID);
            Assert.AreEqual("USD", subjectUnderTest.Accounts[1].Currency);
            Assert.AreEqual((decimal)80.2301373066930000, subjectUnderTest.Accounts[1].Balance);
            Assert.AreEqual((decimal)79.2266348066930000, subjectUnderTest.Accounts[1].Available);
            Assert.AreEqual((decimal)1.0035025000000000, subjectUnderTest.Accounts[1].Hold);
        }
        [TestMethod]
        public void UpdateProducts_ShouldReturnProducts_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"[{{
                            ""id"": ""BTC-EUR"",
                            ""base_currency"": ""BTC"",
                            ""quote_currency"": ""EUR"",
                            ""base_min_size"": ""0.001"",
                            ""base_max_size"": ""10000.00"",
                            ""quote_increment"": ""0.01""
                            }},
                            {{
                            ""id"": ""ETH-EUR"",
                            ""base_currency"": ""ETH"",
                            ""quote_currency"": ""EUR"",
                            ""base_min_size"": ""0.001"",
                            ""base_max_size"": ""10000.00"",
                            ""quote_increment"": ""0.01""
                            }}]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            Coinbase subjectUnderTest = new Coinbase(_authentication, httpClient);
            //Act
            subjectUnderTest.UpdateProductsAsync().Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Products);
            Assert.AreEqual(2, subjectUnderTest.Products.Count);
            Assert.AreEqual("BTC-EUR", subjectUnderTest.Products[0].ID);
        }
        [TestMethod]
        public void UpdateTickers_ShouldReturnProductsAndTickers_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"{{""price"": ""333.99"",""trade_id"": 4729088,""size"": ""0.193"",""bid"": ""333.98"",""ask"": ""333.99"",""volume"": ""5957.11914015"",""time"": ""2015-11-14T20:46:03.511254Z""}}")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            Coinbase subjectUnderTest = new Coinbase(_authentication, httpClient);
            List<Product> products = new List<Product> 
            {
                new Product
                {
                    ID = "BTC-EUR",
                    Base_Currency = "BTC",
                    Quote_Currency = "EUR",
                    Base_Min_Size = "0.001",
                    Base_Max_Size = "10000.00",
                    Quote_Increment = "0.01"
                }
            };
            //Act
            subjectUnderTest.UpdateTickersAsync(products).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Tickers);
            Assert.AreEqual(1, subjectUnderTest.Tickers.Count);
            Assert.AreEqual("BTC-EUR", subjectUnderTest.Tickers[0].ProductID);
            Assert.AreEqual(4729088, subjectUnderTest.Tickers[0].Trade_ID);
            Assert.AreEqual((decimal)333.99, subjectUnderTest.Tickers[0].Price.ToDecimal());
            Assert.AreEqual((decimal)0.193, subjectUnderTest.Tickers[0].Size.ToDecimal());
            Assert.AreEqual((decimal)333.98, subjectUnderTest.Tickers[0].Bid.ToDecimal());
            Assert.AreEqual((decimal)333.99, subjectUnderTest.Tickers[0].Ask.ToDecimal());
            Assert.AreEqual((decimal)5957.11914015, subjectUnderTest.Tickers[0].Volume.ToDecimal());
            Assert.AreEqual("2015-11-14T20:46:03.511254Z".ToDateTime(), subjectUnderTest.Tickers[0].Time.ToDateTime());
            Assert.IsNotNull(subjectUnderTest.CurrentPrices);
            Assert.AreEqual(subjectUnderTest.CurrentPrices[subjectUnderTest.Tickers[0].ProductID], subjectUnderTest.Tickers[0].Price.ToDecimal());
        }
        [TestMethod]
        public void UpdateFills_ShouldReturnFills_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"[{{
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
                            }},
                            {{
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
                            }}]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            Coinbase subjectUnderTest = new Coinbase(_authentication, httpClient);
            Product product = new Product
            {
                ID = "BTC-EUR",
                Base_Currency = "BTC",
                Quote_Currency = "EUR",
                Base_Min_Size = "0.001",
                Base_Max_Size = "10000.00",
                Quote_Increment = "0.01"
            };

            //Act
            subjectUnderTest.UpdateFillsAsync(product).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Fills);
            Assert.AreEqual(2, subjectUnderTest.Fills.Count);
            Assert.AreEqual(product.ID, subjectUnderTest.Fills[0].Product_ID);
        }
        [TestMethod]
        public void UpdateProductOrderBook_ShouldReturnOrderBook_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"{{
                            ""sequence"": ""3"",
                            ""bids"": [[""152.02"",""0.0509"",1],[""151.9"",""1.6256"",1],[""151.89"",""12.8585"",1]],
                            ""asks"": [[""152.37"", ""5"", 1],[""152.38"",""12.8561"",1]]
                            }}")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            Coinbase subjectUnderTest = new Coinbase(_authentication, httpClient);
            Product product = new Product
            {
                ID = "BTC-EUR",
                Base_Currency = "BTC",
                Quote_Currency = "EUR",
                Base_Min_Size = "0.001",
                Base_Max_Size = "10000.00",
                Quote_Increment = "0.01"
            };

            //Act
            subjectUnderTest.UpdateProductOrderBookAsync(product).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.OrderBook);
            Assert.AreEqual(3, subjectUnderTest.OrderBook.Bids.ToOrderList().Count);
            Assert.AreEqual(2, subjectUnderTest.OrderBook.Asks.ToOrderList().Count);
            Assert.AreEqual((decimal)152.02, subjectUnderTest.OrderBook.Bids.ToOrderList()[0].Price.ToDecimal());
            Assert.AreEqual("5", subjectUnderTest.OrderBook.Asks.ToOrderList()[0].Size);
        }
        [TestMethod]
        public void UpdateProductHistoricCandles_ShouldReturnOrderBook_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"[[1415398768, 0.32, 4.2, 0.35, 4.2, 12.3]]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            Coinbase subjectUnderTest = new Coinbase(_authentication, httpClient);
            Product product = new Product
            {
                ID = "BTC-EUR",
                Base_Currency = "BTC",
                Quote_Currency = "EUR",
                Base_Min_Size = "0.001",
                Base_Max_Size = "10000.00",
                Quote_Increment = "0.01"
            };
            DateTime startingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
            DateTime endingDateTime = startingDateTime.AddMonths(6).ToUniversalTime();
            //Act
            subjectUnderTest.UpdateProductHistoricCandlesAsync(product, startingDateTime, endingDateTime).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.HistoricRates);
            Assert.AreEqual(1, subjectUnderTest.HistoricRates.Count);
            Assert.AreEqual((decimal)0.32, subjectUnderTest.HistoricRates[0].Low);
            Assert.AreEqual((decimal)12.3, subjectUnderTest.HistoricRates[0].Volume);
        }
    }
}
