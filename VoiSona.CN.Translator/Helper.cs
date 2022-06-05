namespace VoiSona.CN.Translator
{
    internal static class Helper
    {
        //https://stackoverflow.com/a/31107925
        internal static unsafe long FindIndexOf(this byte[] haystack, byte[] needle, long startOffset = 0)
        {
            fixed (byte* h = haystack) fixed (byte* n = needle)
            {
                for (byte* hNext = h + startOffset, hEnd = h + haystack.LongLength + 1 - needle.LongLength, nEnd = n + needle.LongLength; hNext < hEnd; hNext++)
                for (byte* hInc = hNext, nInc = n; *nInc == *hInc; hInc++)
                    if (++nInc == nEnd)
                        return hNext - h;
                return -1;
            }
        }
    }
}
