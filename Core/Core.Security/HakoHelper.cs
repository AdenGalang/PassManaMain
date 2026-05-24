using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;

namespace PassManaAlpha.Core.Scurity
{
    public static class HakoHelper
    {
        private const byte VERSION_PBKDF2 = 0x01;
        private const byte VERSION_ARGON2ID = 0x02;
        private const int ARGON2_TIME_COST = 1;
        private const int ARGON2_MEMORY_COST = 8192; //RAM -32bit integer-;KB = 1024 bytes
        private const int ARGON2_HASH_LENGTH = 64;    

        private static (byte[] aesKey, byte[] hmacKey) DeriveKeysArgon2(string password, byte[] salt)
        {
            var config = new Argon2Config
            {
                Type = Argon2Type.DataIndependentAddressing,
                Version = Argon2Version.Nineteen,
                Password = Encoding.UTF8.GetBytes(password),
                Salt = salt,
                TimeCost = ARGON2_TIME_COST,
                MemoryCost = ARGON2_MEMORY_COST,
                Lanes = Environment.ProcessorCount,
                Threads = Environment.ProcessorCount,
                HashLength = ARGON2_HASH_LENGTH
            };

            using var argon2 = new Argon2(config);
            using var hash = argon2.Hash();
            byte[] aesKey = hash.Buffer[..32];
            byte[] hmacKey = hash.Buffer[32..64];
            return (aesKey, hmacKey);
        }

        private static byte[] LegacyDeriveKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32);
        }

        private static byte[] LegacyDeriveHmacKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            pbkdf2.GetBytes(32);
            return pbkdf2.GetBytes(32);
        }

        // ─── Encrypt ─────────────────────────────────────────────────────────────

        public static string Encrypt(string plainText, string password)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                throw new ArgumentException("Cannot encrypt empty or null plaintext");

            using var aes = Aes.Create();
            aes.GenerateIV();
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            var (aesKey, hmacKey) = DeriveKeysArgon2(password, salt);
            aes.Key = aesKey;

            using var ms = new MemoryStream();

            ms.WriteByte(VERSION_ARGON2ID);
            ms.Write(salt, 0, salt.Length);
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var encryptor = aes.CreateEncryptor())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
                sw.Flush();
                cs.FlushFinalBlock();
            }

            byte[] cipherBytes = ms.ToArray();

            byte[] hmac;
            using (var hmacSha = new HMACSHA256(hmacKey))
                hmac = hmacSha.ComputeHash(cipherBytes);

            byte[] final = new byte[32 + cipherBytes.Length];
            Buffer.BlockCopy(hmac, 0, final, 0, 32);
            Buffer.BlockCopy(cipherBytes, 0, final, 32, cipherBytes.Length);

            return Convert.ToBase64String(final);
        }

        // ─── Decrypt ─────────────────────────────────────────────────────────────

        public static string? Decrypt(string cipherText, string password)
        {
            try
            {
                byte[] fullData = Convert.FromBase64String(cipherText);

                if (fullData.Length < 66)
                    return null;

                byte[] storedHmac = fullData[..32];
                byte[] cipherBytes = fullData[32..];

                byte version = cipherBytes[0];
                byte[] salt = cipherBytes[1..17];
                byte[] iv = cipherBytes[17..33];
                byte[] encrypted = cipherBytes[33..];

                byte[] aesKey;
                byte[] hmacKey;

                if (version == VERSION_ARGON2ID)
                {
                    var keys = DeriveKeysArgon2(password, salt);
                    aesKey = keys.aesKey;
                    hmacKey = keys.hmacKey;
                }
                else if (version == VERSION_PBKDF2)
                {
                    aesKey = LegacyDeriveKey(password, salt);
                    hmacKey = LegacyDeriveHmacKey(password, salt);
                }
                else
                {
                    Debug.WriteLine("Unknown version byte — cannot decrypt");
                    return null;
                }

                byte[] computedHmac;
                using (var hmacSha = new HMACSHA256(hmacKey))
                    computedHmac = hmacSha.ComputeHash(cipherBytes);

                if (!CryptographicOperations.FixedTimeEquals(storedHmac, computedHmac))
                    return null; 

                using var aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = iv;

                using var ms = new MemoryStream(encrypted);
                using var decryptor = aes.CreateDecryptor();
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs, Encoding.UTF8);
                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Decrypt failed: {ex.Message}");
                return null;
            }
        }

        // ─── Async wrappers ───────────────────────────────────────────────────────

        public static async Task<string> EncryptAsync(string plainText, string password)
            => await Task.Run(() => Encrypt(plainText, password));

        public static async Task<string?> DecryptAsync(string cipherText, string password)
            => await Task.Run(() => Decrypt(cipherText, password));

        public static async Task<string?[]> DecryptManyAsync(IEnumerable<string> cipherTexts, string password)
        {
            var tasks = cipherTexts.Select(ct => Task.Run(() => Decrypt(ct, password)));
            return await Task.WhenAll(tasks);
        }
    }
}