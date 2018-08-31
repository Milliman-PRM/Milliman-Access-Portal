using System;
using System.Collections.Generic;
using System.Text;

namespace TestResourcesLib
{
    public class TestUtil
    {
        public static Guid MakeTestGuid(int Val)
        {
            return new Guid(Val, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        }
    }
}
