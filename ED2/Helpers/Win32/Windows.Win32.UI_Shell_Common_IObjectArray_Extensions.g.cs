﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

#pragma warning disable CS1591,CS1573,CS0465,CS0649,CS8019,CS1570,CS1584,CS1658,CS0436,CS8981
using global::System;
using global::System.Diagnostics;
using global::System.Diagnostics.CodeAnalysis;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;
using global::System.Runtime.Versioning;
using winmdroot = global::Windows.Win32;
namespace Windows.Win32
{
	[global::System.CodeDom.Compiler.GeneratedCode("Microsoft.Windows.CsWin32", "0.2.164-beta+187018bf44")]
	internal static partial class UI_Shell_Common_IObjectArray_Extensions
	{
		/// <inheritdoc cref="winmdroot.UI.Shell.Common.IObjectArray.GetCount(uint*)"/>
		internal static unsafe void GetCount(this winmdroot.UI.Shell.Common.IObjectArray @this, out uint pcObjects)
		{
			fixed (uint* pcObjectsLocal = &pcObjects)
			{
				@this.GetCount(pcObjectsLocal);
			}
		}

		/// <inheritdoc cref="winmdroot.UI.Shell.Common.IObjectArray.GetAt(uint, global::System.Guid*, out object)"/>
		internal static unsafe void GetAt(this winmdroot.UI.Shell.Common.IObjectArray @this, uint uiIndex, in global::System.Guid riid, out object ppv)
		{
			fixed (global::System.Guid* riidLocal = &riid)
			{
				@this.GetAt(uiIndex, riidLocal, out ppv);
			}
		}
	}
}
