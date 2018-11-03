using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public static class Assert
{
    public static void That(bool b, string message)
    {
        if (!b)
        {
            throw new Exception(message);
        }
    }
}
