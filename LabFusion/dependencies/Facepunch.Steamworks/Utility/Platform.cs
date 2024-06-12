﻿using System.Runtime.InteropServices;

namespace Steamworks
{
    internal static class Platform
    {
        // BONELAB is win64, so we just use this
        // #if PLATFORM_WIN64
        public const int StructPlatformPackSize = 8;
        public const string LibraryName = "steam_api64";
        // #elif PLATFORM_WIN32
        // 		public const int StructPlatformPackSize = 8;
        // 		public const string LibraryName = "steam_api";
        // #elif PLATFORM_POSIX
        // 		public const int StructPlatformPackSize = 4;
        // 		public const string LibraryName = "libsteam_api";
        // #endif

        public const CallingConvention CC = CallingConvention.Cdecl;
        public const int StructPackSize = 4;
    }
}
