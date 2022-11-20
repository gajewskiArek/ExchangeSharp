/*
MIT LICENSE

Copyright 2020 Digital Ruby, LLC - http://www.digitalruby.com

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeSharp.API.Exchanges.Chiliz
{
	public class ExchangeChilizAPI : ExchangeAPI
	{
		public override string BaseUrl { get; set; } = "https://api.chiliz.net/openapi";
		public override string BaseUrlWebSocket { get; set; } = "wss://wsapi.chiliz.net/openapi/ws/";

		public ExchangeChilizAPI()
		{
			NonceStyle = NonceStyle.UnixMilliseconds;
		}

		protected internal override async Task<IEnumerable<ExchangeMarket>> OnGetMarketSymbolsMetadataAsync()
		{
			/*
				{
					"filters": [
						{
							"minPrice": "0.01",
							"maxPrice": "100000.00000000",
							"tickSize": "0.01",
							"filterType": "PRICE_FILTER"
						},
						{
							"minQty": "0.0002",
							"maxQty": "100000.00000000",
							"stepSize": "0.000001",
							"filterType": "LOT_SIZE"
						},
						{
							"minNotional": "10",
							"filterType": "MIN_NOTIONAL"
						}
					],
					"exchangeId": "301",
					"symbol": "BTCUSDT",
					"symbolName": "BTCUSDT",
					"status": "TRADING",
					"baseAsset": "BTC",
					"baseAssetName": "BTC",
					"baseAssetPrecision": "0.000001",
					"quoteAsset": "USDT",
					"quoteAssetName": "USDT",
					"quotePrecision": "0.01",
					"icebergAllowed": false,
					"isAggregate": false,
					"allowMargin": false
				},
			*/

			var markets = new List<ExchangeMarket>();
			JToken obj = await MakeJsonRequestAsync<JToken>("v1/brokerInfo");

			if (!(obj is null))
			{
				foreach (JToken marketSymbolToken in obj["symbols"])
				{
					var filters = marketSymbolToken["filters"];
					var filterProperties = filters.Children().SelectMany(x => x.Children()).Cast<JProperty>().ToList();

					var market = new ExchangeMarket
					{
						MarketSymbol = marketSymbolToken["symbol"].ToStringUpperInvariant(),
						IsActive = marketSymbolToken["status"].ToStringUpperInvariant() == "TRADING",
						QuoteCurrency = marketSymbolToken["quoteAsset"].ToStringUpperInvariant(),
						BaseCurrency = marketSymbolToken["baseAsset"].ToStringUpperInvariant(),
						PriceStepSize = filterProperties.First(x => x.Name == "tickSize").Value.ConvertInvariant<decimal>(),
						QuantityStepSize = filterProperties.First(x => x.Name == "stepSize").Value.ConvertInvariant<decimal>(),
						MinTradeSize = filterProperties.First(x => x.Name == "minQty").Value.ConvertInvariant<decimal>(),
						MaxTradeSize = filterProperties.First(x => x.Name == "maxQty").Value.ConvertInvariant<decimal>(),
						MaxTradeSizeInQuoteCurrency = filterProperties.First(x => x.Name == "minNotional").Value.ConvertInvariant<decimal>(),
						MinPrice = filterProperties.First(x => x.Name == "minPrice").Value.ConvertInvariant<decimal>(),
						MaxPrice = filterProperties.First(x => x.Name == "maxPrice").Value.ConvertInvariant<decimal>(),
						MarginEnabled = marketSymbolToken["allowMargin"].ConvertInvariant<bool>(),
					};

					markets.Add(market);
				}
			}

			return markets;
		}

		protected override async Task<IEnumerable<KeyValuePair<string, ExchangeTicker>>> OnGetTickersAsync()
		{
			var json = await MakeJsonRequestAsync<JToken>($"quote/v1/ticker/bookTicker");

			var tickers = json.Select(tickerToken => ParseTicker(tickerToken))
				.Select(ticker => new KeyValuePair<string, ExchangeTicker>(ticker.MarketSymbol, ticker))
				.ToList();

			return tickers;
		}

		protected override async Task<ExchangeTicker> OnGetTickerAsync(string symbol)
		{
			var json = await MakeJsonRequestAsync<JToken>($"quote/v1/ticker/bookTicker?symbol={symbol}");
			return ParseTicker(json);
		}

		private ExchangeTicker ParseTicker(JToken tickerToken)
		{
			bool IsEmptyString(JToken token) => token.Type == JTokenType.String && token.ToObject<string>() == string.Empty;

			/*
					{
					  "symbol": "LTCBTC",
					  "bidPrice": "4.00000000",
					  "bidQty": "431.00000000",
					  "askPrice": "4.00000200",
					  "askQty": "9.00000000"
					}
			*/

			return new ExchangeTicker
			{
				Exchange = Name,
				ApiResponse = tickerToken,
				MarketSymbol = tickerToken["symbol"].ToStringInvariant(),
				Bid = IsEmptyString(tickerToken["bidPrice"]) ? default : tickerToken["bidPrice"].ConvertInvariant<decimal>(),
				Ask = IsEmptyString(tickerToken["askPrice"]) ? default : tickerToken["askPrice"].ConvertInvariant<decimal>(),
			};
		}

		protected override bool CanMakeAuthenticatedRequest(IReadOnlyDictionary<string, object> payload)
		{
			return !(PublicApiKey is null) && !(PrivateApiKey is null);
		}

		protected override Uri ProcessRequestUrl(UriBuilder url, Dictionary<string, object> payload, string method)
		{
			if (CanMakeAuthenticatedRequest(payload) && IsUrlSigned(url.Uri))
			{
				var nonce = GenerateNonceAsync().Result;

				payload.Add("timestamp", nonce);

				url.AppendPayloadToQuery(payload);

				var sign = CryptoUtility.SHA256Sign(url.Query.Substring(1), PrivateApiKey.ToUnsecureBytesUTF8()).UrlEncode();

				url.Query += $"&signature={sign}";
			}
			return url.Uri;
		}

		private bool IsUrlSigned(Uri url)
		{
			var signegEndpoints = new[] { "v1/userDataStream", "v1/order", "v1/order/test", "v1/openOrders", "v1/historyOrders", "v1/account", "v1/myTrades", "v1/depositOrders" };

			return signegEndpoints.Any(x => url.AbsolutePath.EndsWith(x));
		}

		protected override Task ProcessRequestAsync(IHttpWebRequest request, Dictionary<string, object>? payload)
		{
			if (CanMakeAuthenticatedRequest(payload)
				&& IsUrlSigned(request.RequestUri)
				//|| (payload == null && request.RequestUri.AbsoluteUri.Contains("userDataStream"))
				)
			{
				request.AddHeader("X-BH-APIKEY", PublicApiKey!.ToUnsecureString());
			}

			return base.ProcessRequestAsync(request, payload);
		}
	}
}
