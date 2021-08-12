using SharpPcap;

namespace BlitzSniffer.Util
{
    public static class PosixTimevalExtensions
    {
        private const ulong ONE_SECOND_IN_MICROSECONDS = 1000000;

        public static ulong ToTotalMicroseconds(this PosixTimeval timeval)
        {
            return (timeval.Seconds * ONE_SECOND_IN_MICROSECONDS) + timeval.MicroSeconds;
        }

        public static PosixTimeval ToPosixTimeval(this ulong microseconds)
        {
            return new PosixTimeval(microseconds / ONE_SECOND_IN_MICROSECONDS, microseconds % ONE_SECOND_IN_MICROSECONDS);
        }

        private static PosixTimeval Add(this PosixTimeval a, ulong microSeconds, bool minus = false)
        {
            ulong aMicroseconds = a.ToTotalMicroseconds();

            ulong totalMicroseconds;
            if (!minus)
            {
                totalMicroseconds = aMicroseconds + microSeconds;
            }
            else
            {
                totalMicroseconds = aMicroseconds - microSeconds;
            }

            ulong seconds = totalMicroseconds / ONE_SECOND_IN_MICROSECONDS;

            return new PosixTimeval(seconds, totalMicroseconds - (seconds * ONE_SECOND_IN_MICROSECONDS));
        }

        // Operators are not supported as extensions, so this is the next best thing.
        // Tracking: https://github.com/dotnet/csharplang/issues/192
        public static PosixTimeval Add(this PosixTimeval a, PosixTimeval b)
        {
            return a.Add(b.ToTotalMicroseconds(), false);
        }

        public static PosixTimeval Subtract(this PosixTimeval a, PosixTimeval b)
        {
            return a.Add(b.ToTotalMicroseconds(), true);
        }

        public static PosixTimeval Add(this PosixTimeval a, ulong b)
        {
            return a.Add(b, false);
        }

        public static PosixTimeval Subtract(this PosixTimeval a, ulong b)
        {
            return a.Add(b, true);
        }

    }
}
