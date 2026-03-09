using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace EventBusSystem.SaveSystem
{
    public static class SaveSystemConfig
    {
        public const int CurrentSaveVersion = 1;
        public const int MaxSlots = 5;
        public const string SaveFileExtension = ".sav";
        public const string MetaFileExtension = ".meta";
        public const string SaveFilePrefix = "slot_";
        public const float AutoSaveIntervalSeconds = 300f;
        public const int AutoSaveSlotIndex = 0;

        // Machine-derived AES key + IV
        // Computed once and cached.
        // Key: SHA-256 of (MachineGUID + AppIdentifier) → 32 bytes
        // IV : first 16 bytes of MD5 of (ProductName + CompanyName)

        static byte[] _key;
        static byte[] _iv;

        /// <summary>
        /// A stable app identifier baked into the build.
        /// Change this if you ever need to invalidate all existing saves.
        /// </summary>
        const string AppSalt = "MyGame_v1"; // <-- change per project

        public static byte[] EncryptionKey
        {
            get
            {
                if (_key != null) return _key;

                string raw = SystemInfo.deviceUniqueIdentifier + AppSalt;
                using (var sha = SHA256.Create())
                {
                    _key = sha.ComputeHash(Encoding.UTF8.GetBytes(raw)); //32 bytes
                }
                return _key;
            }
        }

        public static byte[] EncryptionIV
        {
            get
            {
                if (_iv != null) return _iv;

                // IV doesn't need to be secret, just consistent per installation.
                // MD5 gives us exactly 16 bytes.
                string raw = Application.productName + Application.companyName + AppSalt;
                using (var md5 = MD5.Create())
                {
                    _iv = md5.ComputeHash(Encoding.UTF8.GetBytes(raw)); // → 16 bytes
                }
                return _iv;
            }
        }
    }
}