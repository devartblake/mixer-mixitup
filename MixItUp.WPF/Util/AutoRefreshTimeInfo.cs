using static MixItUp.WPF.Util.AutoRefreshTime;

namespace MixItUp.WPF.Util
{
    public class AutoRefreshTimeInfo
    {
        public int Value { get; set; }
        public TimeUnit TimeUnit { get; set; }

        public AutoRefreshTimeInfo() { }

        public AutoRefreshTimeInfo(int value, TimeUnit timeUnit)
        {
            Value = value;
            TimeUnit = timeUnit;
        }
    }
}
