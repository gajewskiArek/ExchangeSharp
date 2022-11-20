/*
MIT LICENSE

Copyright 2020 Digital Ruby, LLC - http://www.digitalruby.com

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
