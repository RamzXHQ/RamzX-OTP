// RamzX OTP - KeePass 2 plugin
// Copyright (c) 2026 RamzX. MIT License.

using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RamzXOtp.Integration
{
    /// <summary>
    /// Finds the RamzX Unlock plugin at run time (no compile-time dependency) and uses its
    /// public MiftawerkUnlockApi to ask for Windows Hello. Both plugins keep working alone.
    /// </summary>
    internal sealed class UnlockBridge
    {
        internal const string AssemblyName = "MiftawerkUnlock";
        internal const string ApiTypeName = "MiftawerkUnlock.MiftawerkUnlockApi";
        internal const int SupportedApiVersion = 1;

        private readonly MethodInfo m_verify;

        private UnlockBridge(MethodInfo verify) { m_verify = verify; }

        /// <summary>The bridge to the loaded RamzX Unlock plugin, or null if it is not loaded.</summary>
        internal static UnlockBridge Find()
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(a.GetName().Name, AssemblyName, StringComparison.OrdinalIgnoreCase)) continue;
                UnlockBridge b = Bind(a.GetType(ApiTypeName, false));
                if (b != null) return b;
            }
            return null;
        }

        /// <summary>Checks the API version and the method signature; null if incompatible.</summary>
        internal static UnlockBridge Bind(Type api)
        {
            if (api == null) return null;
            FieldInfo version = api.GetField("ApiVersion", BindingFlags.Public | BindingFlags.Static);
            if (version == null || !(version.GetValue(null) is int) || (int)version.GetValue(null) != SupportedApiVersion)
                return null;

            MethodInfo verify = api.GetMethod("VerifyUserAsync", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(IWin32Window), typeof(string) }, null);
            if (verify == null || verify.ReturnType != typeof(Task<bool>)) return null;
            return new UnlockBridge(verify);
        }

        /// <summary>Windows Hello prompt via RamzX Unlock; false on cancel or error.</summary>
        internal async Task<bool> VerifyAsync(IWin32Window owner, string message)
        {
            try
            {
                Task<bool> t = m_verify.Invoke(null, new object[] { owner, message }) as Task<bool>;
                return t != null && await t;
            }
            catch (Exception) { return false; }
        }
    }
}
