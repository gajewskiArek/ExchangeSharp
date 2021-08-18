using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using ExchangeSharp;
using ExchangeSharpConsole.Options.Interfaces;

namespace ExchangeSharpConsole.Options
{
	[Verb("ws-tickers", HelpText =
		"Connects to the given exchange websocket and keeps printing tickers from that exchange.\n" +
		"If market symbol is not set then uses all.")]
	public class WebSocketsTickersOption : BaseOption, IOptionPerExchange, IOptionWithMultipleMarketSymbol
	{
		public override async Task RunCommand()
		{
			async Task<IWebSocket> GetWebSocket(IExchangeAPI api)
			{
				var symbols = await ValidateMarketSymbolsAsync(api, MarketSymbols.ToArray(), true);

				return await api.GetTickersWebSocketAsync(freshTickers =>
					{
						foreach (var (key, ticker) in freshTickers)
						{
							//if (key == "USDT_BTC")
								if (key.Contains("BTC") && key.Contains("USD"))
								{
									Console.WriteLine($"{DateTime.Now.ToLongTimeString()} {api.Name} Market {key,8}: Ticker {ticker}, " +
									$"{ticker.Volume.QuoteCurrencyVolume} {ticker.Volume.QuoteCurrency} / {ticker.Volume.BaseCurrencyVolume} {ticker.Volume.BaseCurrency}");
							}
						}
					},
					symbols
				);
			}

			var e1 = "Binance";
			var e2 = "Poloniex";

			var t1 = RunWebSocket(e1, GetWebSocket);
		//	var t2 = RunWebSocket(e2, GetWebSocket);

			Task.WaitAll(t1);
		}

		public string ExchangeName { get; set; }

		public IEnumerable<string> MarketSymbols { get; set; }
	}
}
