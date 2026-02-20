using System;
using System.Diagnostics;

namespace keyboard_unchatter_csharp
{
    internal class Debug
    {
        [Conditional("DEBUG")]
        public static void Log(string text)
        {
            Console.WriteLine(text);
        }
    }
}
