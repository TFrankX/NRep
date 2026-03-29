using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

class Program
{
    static void Main()
    {
        var mac = GetMACAddress();
        var hostname = GetHostname();
        var username = GetUsername();

        Console.WriteLine($"MAC address: {mac}");
        Console.WriteLine($"Hostname:    {hostname}");
        Console.WriteLine($"Username:    {username}");

        var identity = $"getid-v2:{mac}:{hostname}:{username}:secure-token-storage";
        Console.WriteLine($"Identity:    {identity}");

        using var sha256 = SHA256.Create();
        var key = sha256.ComputeHash(Encoding.UTF8.GetBytes(identity));
        Console.WriteLine($"Key (hex):   {BitConverter.ToString(key).Replace("-", "").ToLower()}");

        // Try to read and decrypt a secret
        var storePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "store");
        var secretFile = Path.Combine(storePath, "sec_StageDbDevicePass");

        if (File.Exists(secretFile))
        {
            Console.WriteLine($"\nTrying to decrypt: {secretFile}");
            var encrypted = File.ReadAllText(secretFile).Trim();
            try
            {
                var decrypted = Decrypt(encrypted, key);
                Console.WriteLine($"Decrypted: {decrypted}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decrypt error: {ex.Message}");
            }
        }
    }

    static string GetMACAddress()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "";
        return "";
    }

    static string GetHostname()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var hostnameFile = "/etc/hostname";
            if (File.Exists(hostnameFile))
                return File.ReadAllText(hostnameFile).Trim();
            return System.Net.Dns.GetHostName();
        }
        return Environment.MachineName;
    }

    static string GetUsername()
    {
        var user = Environment.GetEnvironmentVariable("USER");
        if (!string.IsNullOrEmpty(user)) return user;
        user = Environment.GetEnvironmentVariable("LOGNAME");
        if (!string.IsNullOrEmpty(user)) return user;
        return Environment.UserName;
    }

    static string Decrypt(string encrypted, byte[] key)
    {
        var data = Convert.FromBase64String(encrypted);
        const int nonceSize = 12;
        const int tagSize = 16;

        var nonce = new byte[nonceSize];
        Array.Copy(data, 0, nonce, 0, nonceSize);

        var ciphertextWithTag = new byte[data.Length - nonceSize];
        Array.Copy(data, nonceSize, ciphertextWithTag, 0, ciphertextWithTag.Length);

        // Use BouncyCastle for AES-GCM
        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            new KeyParameter(key),
            tagSize * 8,
            nonce);

        cipher.Init(false, parameters);
        var plaintext = new byte[cipher.GetOutputSize(ciphertextWithTag.Length)];
        var len = cipher.ProcessBytes(ciphertextWithTag, 0, ciphertextWithTag.Length, plaintext, 0);
        cipher.DoFinal(plaintext, len);

        return Encoding.UTF8.GetString(plaintext);
    }
}
