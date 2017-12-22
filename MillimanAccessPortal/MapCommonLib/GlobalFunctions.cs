using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MapCommonLib
{
    public static class GlobalFunctions
    {
        public static bool IsValidEmail(string TestAddress)
        {
            try
            {
                return Regex.IsMatch(TestAddress,
                    @"^(([^<>()[\]\\.,;:\s@']+(\.[^<>()[\]\\.,;:\s@']+)*)|('.+'))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$"
                    , RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

            }
            catch 
            {
                return false;
            }
        }
    }
}
