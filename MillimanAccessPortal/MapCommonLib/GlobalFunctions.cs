using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace MapCommonLib
{
    public static class GlobalFunctions
    {
        public static bool IsValidEmail(string TestAddress)
        {
            return new EmailAddressAttribute().IsValid(TestAddress);
        }
    }
}
