using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace WebServer.Services
{
    public static class SecretProvider
    {
        private const string Salt = "getid-v2:secure-token-storage";
        private static readonly string StorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "store");

        /// <summary>
        /// Gets a secret by name from ~/.config/store/sec_{name}
        /// </summary>
        public static string? GetSecret(string name)
        {
            try
            {
                var filePath = Path.Combine(StorePath, $"sec_{name}");
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"[SecretProvider] Secret file not found: {filePath}");
                    return null;
                }

                var encrypted = File.ReadAllText(filePath).Trim();
                var key = DeriveKey();
                return Decrypt(encrypted, key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SecretProvider] Error getting secret '{name}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Derives a 32-byte AES key from machine identity using SHA-256
        /// Components: MAC address + hostname + username + salt
        /// </summary>
        private static byte[] DeriveKey()
        {
            var mac = GetMACAddress();
            var hostname = GetHostname();
            var username = GetUsername();

            var identity = $"getid-v2:{mac}:{hostname}:{username}:secure-token-storage";
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(identity));
        }

        /// <summary>
        /// Gets the MAC address of the first suitable network interface
        /// Matches Go's getid behavior exactly - only reads from /sys/class/net on Linux
        /// </summary>
        private static string GetMACAddress()
        {
            // Only try to get MAC on Linux via /sys/class/net (same as Go version)
            // On macOS/Windows, Go returns empty string, so we do the same
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "";
            }

            var interfaces = new[] { "ens160", "eth0", "ens192", "ens33" };
            foreach (var iface in interfaces)
            {
                var path = $"/sys/class/net/{iface}/address";
                if (File.Exists(path))
                {
                    var mac = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(mac) && mac != "00:00:00:00:00:00")
                        return mac;
                }
            }

            // Fallback: try any interface
            var netDir = "/sys/class/net";
            if (Directory.Exists(netDir))
            {
                foreach (var dir in Directory.GetDirectories(netDir))
                {
                    var name = Path.GetFileName(dir);
                    if (name == "lo") continue;

                    var addrPath = Path.Combine(dir, "address");
                    if (File.Exists(addrPath))
                    {
                        var mac = File.ReadAllText(addrPath).Trim();
                        if (!string.IsNullOrEmpty(mac) && mac != "00:00:00:00:00:00")
                            return mac;
                    }
                }
            }

            return "";
        }

        private static string FormatMacAddress(string mac)
        {
            if (mac.Length != 12) return mac.ToLower();

            var sb = new StringBuilder();
            for (int i = 0; i < 12; i += 2)
            {
                if (sb.Length > 0) sb.Append(':');
                sb.Append(mac.Substring(i, 2).ToLower());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the machine hostname (matches Go's os.Hostname())
        /// </summary>
        private static string GetHostname()
        {
            try
            {
                // On Unix-like systems, use the actual hostname command output to match Go
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var hostnameFile = "/etc/hostname";
                    if (File.Exists(hostnameFile))
                    {
                        return File.ReadAllText(hostnameFile).Trim();
                    }

                    // On macOS, use System.Net.Dns which matches Go's os.Hostname()
                    return System.Net.Dns.GetHostName();
                }
                return Environment.MachineName;
            }
            catch
            {
                return "unknown-host";
            }
        }

        /// <summary>
        /// Gets the current username
        /// </summary>
        private static string GetUsername()
        {
            // Try environment variables first (same as Go)
            var user = Environment.GetEnvironmentVariable("USER");
            if (!string.IsNullOrEmpty(user)) return user;

            user = Environment.GetEnvironmentVariable("LOGNAME");
            if (!string.IsNullOrEmpty(user)) return user;

            // Fallback
            try
            {
                return Environment.UserName;
            }
            catch
            {
                return "unknown-user";
            }
        }

        /// <summary>
        /// Decrypts text using AES-256-GCM with BouncyCastle
        /// Format: base64(nonce || ciphertext || tag)
        /// </summary>
        private static string Decrypt(string encrypted, byte[] key)
        {
            var data = Convert.FromBase64String(encrypted);

            // GCM nonce size is 12 bytes
            const int nonceSize = 12;
            // GCM tag size is 16 bytes (128 bits)
            const int tagSize = 16;

            if (data.Length < nonceSize + tagSize)
                throw new ArgumentException("Ciphertext too short");

            var nonce = new byte[nonceSize];
            Array.Copy(data, 0, nonce, 0, nonceSize);

            // The rest is ciphertext + tag (BouncyCastle expects them together)
            var ciphertextWithTag = new byte[data.Length - nonceSize];
            Array.Copy(data, nonceSize, ciphertextWithTag, 0, ciphertextWithTag.Length);

            // Use BouncyCastle for AES-GCM decryption
            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(
                new KeyParameter(key),
                tagSize * 8,  // tag size in bits
                nonce);

            cipher.Init(false, parameters);  // false = decrypt

            var plaintext = new byte[cipher.GetOutputSize(ciphertextWithTag.Length)];
            var len = cipher.ProcessBytes(ciphertextWithTag, 0, ciphertextWithTag.Length, plaintext, 0);
            cipher.DoFinal(plaintext, len);

            return Encoding.UTF8.GetString(plaintext);
        }
    }
}
