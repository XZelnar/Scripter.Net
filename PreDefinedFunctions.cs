using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScripterNet
{
    static class PreDefinedFunctions
    {
        internal static dynamic processBaseFunction(String fn, dynamic p1, dynamic p2)
        {
            if (fn == "abs")
                return Math.Abs(p1);
            if (fn == "acos")
                return Math.Acos(p1);
            if (fn == "asin")
                return Math.Asin(p1);
            if (fn == "atan")
                return Math.Atan(p1);
            if (fn == "atan2")
                return Math.Atan2(p1, p2);
            if (fn == "cos")
                return Math.Cos(p1);
            if (fn == "exp")
                return Math.Exp(p1);
            if (fn == "floor")
                return Math.Floor(p1);
            if (fn == "log")
                return Math.Log(p1, p2);
            if (fn == "max")
                return Math.Max(p1, p2);
            if (fn == "min")
                return Math.Min(p1, p2);
            if (fn == "pow")
                return Math.Pow(p1, p2);
            if (fn == "round")
                return Math.Round(p1);
            if (fn == "sign")
                return Math.Sign(p1);
            if (fn == "sin")
                return Math.Sin(p1);
            if (fn == "sqr")
                return p1 * p1;
            if (fn == "sqrt")
                return Math.Sqrt(p1);
            if (fn == "tan")
                return Math.Tan(p1);

            return null;
        }
    }
}
