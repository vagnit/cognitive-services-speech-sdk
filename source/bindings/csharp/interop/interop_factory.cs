//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;
using static Microsoft.CognitiveServices.Speech.Internal.SpxExceptionThrower;

namespace Microsoft.CognitiveServices.Speech.Internal
{
    using SPXHR = System.IntPtr;
    using SPXRECOHANDLE = System.IntPtr;

    internal static class Import
    {
#if !UNIX
        public const string NativeDllName = "Microsoft.CognitiveServices.Speech.core.dll";
#else
        public const string NativeDllName = "libMicrosoft.CognitiveServices.Speech.core.so";
#endif
    }

    internal class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute() { }
    }

    public delegate IntPtr HandleRelease(IntPtr hresult);
    public sealed class InteropSafeHandle : SafeHandle
    {
        private HandleRelease releaseHandleFunc = null;
        public InteropSafeHandle(IntPtr handle, HandleRelease releaseHandle) : base(handle, true)
        {
            releaseHandleFunc = releaseHandle;
            SetHandle(handle);
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            if (releaseHandleFunc != null)
            {
                LogErrorIfFail(releaseHandleFunc(handle));
                releaseHandleFunc = null;
                handle = IntPtr.Zero;
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            ReleaseHandle();
        }

        public static T GetObjectFromWeakHandle<T>(IntPtr nativeHandle)
        {
            var weakGCHandle = GCHandle.FromIntPtr(nativeHandle);
            if (!weakGCHandle.IsAllocated)
            {
                return default(T);
            }
            return (T)weakGCHandle.Target;
        }
    }

    internal static class SpxFactory
    {
        public delegate IntPtr GetResultDelegate(InteropSafeHandle handle, IntPtr result, UInt32 maxCharCount);

        public static string GetDataFromHandleUsingDelegate(GetResultDelegate functionPtr, InteropSafeHandle handle, Int32 maxCharCount)
        {
            string resultTextStr = string.Empty;
            IntPtr resultTextPtr = Marshal.AllocHGlobal(maxCharCount + 1);
            try
            {
                ThrowIfFail(functionPtr(handle, resultTextPtr, (uint)maxCharCount));
                resultTextStr = Utf8StringMarshaler.MarshalNativeToManaged(resultTextPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(resultTextPtr);
            }
            return resultTextStr;
        }

        [DllImport(Import.NativeDllName, CallingConvention = CallingConvention.StdCall)]
        public static extern SPXHR recognizer_create_speech_recognizer_from_config(out SPXRECOHANDLE recoHandle, InteropSafeHandle speechconfig, InteropSafeHandle audioInput);

        [DllImport(Import.NativeDllName, CallingConvention = CallingConvention.StdCall)]
        public static extern SPXHR recognizer_create_translation_recognizer_from_config(out SPXRECOHANDLE recoHandle, InteropSafeHandle speechconfig, InteropSafeHandle audioInput);

        [DllImport(Import.NativeDllName, CallingConvention = CallingConvention.StdCall)]
        public static extern SPXHR recognizer_create_intent_recognizer_from_config(out SPXRECOHANDLE recoHandle, InteropSafeHandle speechconfig, InteropSafeHandle audioInput);
    }    
}
