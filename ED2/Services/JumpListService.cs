using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.Shell.PropertiesSystem;

namespace ED2.Services;

class JumpListService : IJumpListService
{
    public void Update()
    {
        var customDestinationList = (ICustomDestinationList)new DestinationList();

        try
        {
            Guid IID_IObjectArray = new("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9");
            customDestinationList.BeginList(out var minSlots, IID_IObjectArray, out var removedItemsObject);

            // handle deleted items

            // build the list
            var category = (IObjectCollection)new EnumerableObjectCollection();

            // set the link path and args
            var link = (IShellLinkW)new ShellLink();
            link.SetPath("MB.EasyDownloader2.exe");
            link.SetArguments("a.txt");

            // set the link title
            var linkPropStore = (IPropertyStore)link;
            PROPVARIANT pv = default;
            pv.Anonymous.Anonymous.vt = Windows.Win32.System.Com.VARENUM.VT_LPWSTR;
            unsafe
            {
                nint ps = default;
                try
                {
                    ps = Marshal.StringToHGlobalUni("moop2");
                    pv.Anonymous.Anonymous.Anonymous.pbVal = (byte*)ps;
                    linkPropStore.SetValue(PInvoke.PKEY_Title, pv);
                }
                finally
                {
                    Marshal.FreeHGlobal(ps);
                }
            }

            category.AddObject(link);

            customDestinationList.AppendCategory("moop category 2", category);

            // commit the changes
            customDestinationList.CommitList();
        }
        finally
        {
            Marshal.FinalReleaseComObject(customDestinationList);
        }
    }
}
