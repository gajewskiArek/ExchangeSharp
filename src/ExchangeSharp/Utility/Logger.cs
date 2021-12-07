/*
MIT LICENSE

Copyright 2017 Digital Ruby, LLC - http://www.digitalruby.com

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#region Imports

using Microsoft.Extensions.Logging;
using System;
//using NLog;
//using NLog.Config;

#endregion Imports

namespace ExchangeSharp
{
    /// <summary>
    /// ExchangeSharp logger. Will never throw exceptions.
    /// Currently the ExchangeSharp logger uses NLog internally, so make sure it is setup in your app.config file or nlog.config file.
    /// </summary>
    public static class Logger
    {
		public static ILogger logger { get; set; }

		/// <summary>
		/// Log an error
		/// </summary>
		/// <param name="ex">Error</param>
		public static void Error(Exception ex) => logger.LogError(ex, ex.Message);

		/// <summary>
		/// Log an error
		/// </summary>
		/// <param name="ex">Error</param>
		/// <param name="text">Text with format</param>
		/// <param name="args">Format args</param>
		public static void Error(Exception ex, string text, params object[] args) => logger.LogError(ex, ex.Message, args);

		/// <summary>
		/// Log a warning message
		/// </summary>
		/// <param name="text">Text with format</param>
		/// <param name="args">Format args</param>
		public static void Warn(string text, params object[] args) => logger.LogWarning(text, args);


		/// <summary>
		/// Log an info message
		/// </summary>
		/// <param name="text">Text with format</param>
		/// <param name="args">Format args</param>
		public static void Info(string text, params object[] args) => logger.LogInformation(text, args);

		/// <summary>
		/// Log a debug message
		/// </summary>
		/// <param name="text">Text with format</param>
		/// <param name="args">Format args</param>
		public static void Debug(string text, params object[] args) => logger.LogInformation(text, args);
    }
}
