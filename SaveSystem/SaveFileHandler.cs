using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MessagePack; //IMPORTANT: Make sure to install MessagePack-CSharp from NuGet and add the [MessagePackObject] attribute to your SaveData and SaveSlotMeta classes.
using UnityEngine;

namespace EventBusSystem.SaveSystem
{
    public static class SaveFileHandler
    {
        static byte[] Key => SaveSystemConfig.EncryptionKey;
        static byte[] IV => SaveSystemConfig.EncryptionIV;

        public static string SaveDir => Path.Combine(Application.persistentDataPath, "saves");
        public static string SavePath(int s) => Path.Combine(SaveDir, $"slot_{s}.sav");
        public static string MetaPath(int s) => Path.Combine(SaveDir, $"slot_{s}.meta");

        public static async Task WriteAsync(int slot, SaveData data, SaveSlotMeta meta)
        {
            if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);
            await File.WriteAllBytesAsync(SavePath(slot), Encrypt(MessagePackSerializer.Serialize(data)));
            await File.WriteAllBytesAsync(MetaPath(slot), MessagePackSerializer.Serialize(meta));
        }

        public static async Task<SaveData> ReadAsync(int slot)
        {
            string p = SavePath(slot);
            if (!File.Exists(p)) throw new FileNotFoundException($"Slot {slot} not found.", p);
            return MessagePackSerializer.Deserialize<SaveData>(Decrypt(await File.ReadAllBytesAsync(p)));
        }

        public static async Task<SaveSlotMeta> ReadMetaAsync(int slot)
        {
            string p = MetaPath(slot);
            if (!File.Exists(p)) return new SaveSlotMeta { SlotIndex = slot, IsEmpty = true };
            return MessagePackSerializer.Deserialize<SaveSlotMeta>(await File.ReadAllBytesAsync(p));
        }

        public static void Delete(int slot)
        {
            if (File.Exists(SavePath(slot))) File.Delete(SavePath(slot));
            if (File.Exists(MetaPath(slot))) File.Delete(MetaPath(slot));
        }

        public static bool SlotExists(int slot) => File.Exists(SavePath(slot));

        static byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = Key; aes.IV = IV;
            aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length); cs.FlushFinalBlock();
            return ms.ToArray();
        }

        static byte[] Decrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = Key; aes.IV = IV;
            aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
            using var ms = new MemoryStream(data);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var out_ = new MemoryStream();
            cs.CopyTo(out_); return out_.ToArray();
        }
    }
}