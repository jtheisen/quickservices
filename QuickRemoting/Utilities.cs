using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickRemoting;

static class Utilities
{
    public static T AssertNotNull<T>(this T? value, String message)
        where T : class
    {
        if (value is null) throw new Exception(message);

        return value;
    }
}
