using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MapCommonLib
{
    public static class GlobalFunctions
    {
        static Regex EmailAddressValidationRegex = new Regex (@"^(([^<>()[\]\\.,;:\s@']+(\.[^<>()[\]\\.,;:\s@']+)*)|('.+'))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$",
                                                                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

        public static bool IsValidEmail(string TestAddress)
        {
            try
            {
                return EmailAddressValidationRegex.IsMatch(TestAddress);
            }
            catch 
            {
                return false;
            }
        }
    }
}
