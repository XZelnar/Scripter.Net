using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet
{
    static class ConstantsParser
    {
        public static bool TryParseAnyInt(String s, bool bin, bool hex, bool l, out dynamic res)
        {
            if (bin)
                res = ParseBin(s);
            else if (hex)
                res = ParseHex(s);
            else
            {
                long t;
                try
                {
                    t = ParseLong(s, l);
                }
                catch (Exception e)
                {
                    if (l)
                        throw e;
                    res = 0;
                    return false;
                }
                if (l)
                    res = t;
                else
                {
                    if (t > Int32.MaxValue || t < Int32.MinValue)
                        throw new Exception("Constant \"" + s + "\" is not a valid Int32 constant");
                    res = (int)t;
                }
            }

            return true;
        }

        private static int ParseBin(String s)
        {
            if (s.Length > 32)
                throw new Exception("Binary number length cannot be larger than 32");
            int res = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '0' && s[i] != '1')
                    throw new Exception("Unexpected symbol in binary constant: \"" + s[i] + "\"");
                res <<= 1;
                res += s[i] - '0';
            }
            return res;
        }

        private static int ParseHex(String s)
        {
            String ts = s.ToUpper();
            int res = 0;
            int t;
            for (int i = 0; i < s.Length; i++)
            {
                if (!((ts[i] >= '0' && ts[i] <= '9') || (ts[i] >= 'A' && ts[i] <= 'F')))
                    throw new Exception("Unexpected symbol in hex constant: \"" + s[i] + "\"");
                res <<= 4;
                t = ts[i] >= 'A' ? ts[i] - 'A' : ts[i] - '0';
                res += t;
            }
            return res;
        }

        private static long ParseLong(String s, bool l)
        {
            long res = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] < '0' || s[i] > '9')
                    throw new Exception("Unexpected symbol in dec constant: \"" + s[i] + "\"");
                res *= 10;
                res += s[i] - '0';
            }
            if (res.ToString() != s)
                throw new Exception("Constant \"" + s + "\" is not a valid Int" + (l ? "64" : "32") + " constant");
            return res;
        }
    }
}
