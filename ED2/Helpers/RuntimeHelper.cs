namespace ED2.Helpers;

public class RuntimeHelper
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);

    static bool? isMSIX;
    public static bool IsMSIX
    {
        get
        {
            if (isMSIX.HasValue) return isMSIX.Value;

            var length = 0;
            return (isMSIX = (GetCurrentPackageFullName(ref length, null) != 15700L)).Value;
        }
    }
}
