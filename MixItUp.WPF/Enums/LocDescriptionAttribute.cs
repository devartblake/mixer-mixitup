using MixItUp.WPF.Util;
using System;

namespace MixItUp.WPF.Enums
{
    public class LocDescriptionAttribute : Attribute
    {
        public string LocDescription { get; }
        public LocDescriptionAttribute(string key, bool upper = false)
        {
            LocDescription = LocUtil.Get(key, upper)?.Replace("\\n", Environment.NewLine);
        }
    }
}
