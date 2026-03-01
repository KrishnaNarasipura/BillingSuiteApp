using NumericWordsConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BillingSuite.Domain
{
    public static class Utility
    {
        public static string ConvertNumberToWords(decimal number)
        {

            var currencyConverter = new CurrencyWordsConverter(
                new CurrencyWordsConversionOptions()
                {
                    Culture = Culture.Nepali,      // Indian numbering system
                    OutputFormat = OutputFormat.English
                });

            string words = "Rupees "+currencyConverter.ToWords(number).Replace("rupees","");

            return words;

        }
    }
}
