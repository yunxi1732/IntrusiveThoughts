using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// 不依赖 Unity 原生插件，PC / Android / iOS 共用同一格式。
public static class SaveFileStorage
{
    private static readonly byte[] Header = Encoding.ASCII.GetBytes("ITSAVE01");
    private const int SaltSize = 16, IvSize = 16, TagSize = 32;
    private const int Iterations = 100000;

    public static bool Exists(string path) => File.Exists(path) || File.Exists(path + ".bak");

    public static string ReadText(string path, string password)
    {
        try
        {
            return Decode(File.ReadAllBytes(path), password);
        }
        catch (Exception error) when (error is IOException || error is CryptographicException || error is ArgumentException)
        {
            // 两次重命名之间被系统终止时，仍能读取上一次完整写入的文件。
            if (!File.Exists(path + ".bak")) throw;
            return Decode(File.ReadAllBytes(path + ".bak"), password);
        }
    }

    public static void WriteText(string path, string text, string password)
    {
        byte[] data = Encode(text, password);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + ".tmp";
        string backup = path + ".bak";
        WriteBytes(temporary, data);
        // 同目录重命名，不依赖不同移动端运行时对 File.Replace 的支持。
        if (File.Exists(path))
        {
            // 不让损坏的主文件覆盖上一次有效备份。
            try
            {
                byte[] previous = File.ReadAllBytes(path);
                string previousText = Decode(previous, password);
                if (previous.Length >= Header.Length && previous[0] == Header[0])
                {
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(path, backup);
                }
                else
                {
                    // 迁移旧明文时，备份也加密，避免旁边留下可直接阅读的副本。
                    WriteBytes(backup, Encode(previousText, password));
                    File.Delete(path);
                }
            }
            catch (CryptographicException) { File.Delete(path); }
        }
        try { File.Move(temporary, path); }
        catch
        {
            if (!File.Exists(path) && File.Exists(backup)) File.Move(backup, path);
            throw;
        }
    }

    private static void WriteBytes(string path, byte[] data)
    {
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(data, 0, data.Length);
            stream.Flush(true);
        }
    }

    public static byte[] Encode(string text, string password)
    {
        byte[] salt = new byte[SaltSize];
        byte[] iv = new byte[IvSize];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(salt);
            random.GetBytes(iv);
        }
        byte[] keys = DeriveKeys(password, salt);
        byte[] cipher;
        using (var aes = Aes.Create())
        {
            aes.Key = Slice(keys, 0, 32);
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            byte[] plain = Encoding.UTF8.GetBytes(text);
            using (var encryptor = aes.CreateEncryptor()) cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        }
        byte[] output = new byte[Header.Length + SaltSize + IvSize + cipher.Length + TagSize];
        Buffer.BlockCopy(Header, 0, output, 0, Header.Length);
        Buffer.BlockCopy(salt, 0, output, Header.Length, SaltSize);
        Buffer.BlockCopy(iv, 0, output, Header.Length + SaltSize, IvSize);
        Buffer.BlockCopy(cipher, 0, output, Header.Length + SaltSize + IvSize, cipher.Length);
        using (var hmac = new HMACSHA256(Slice(keys, 32, 32)))
        {
            byte[] tag = hmac.ComputeHash(output, 0, output.Length - TagSize);
            Buffer.BlockCopy(tag, 0, output, output.Length - TagSize, TagSize);
        }
        return output;
    }

    public static string Decode(byte[] data, string password)
    {
        // 兼容已有明文 JSON，之后手动保存时转成加密格式。
        int first = 0;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) first = 3;
        while (first < data.Length && char.IsWhiteSpace((char)data[first])) first++;
        if (first < data.Length && data[first] == '{') return Encoding.UTF8.GetString(data, first, data.Length - first);
        if (data.Length < Header.Length + SaltSize + IvSize + 16 + TagSize)
            throw new CryptographicException("存档内容不完整。");
        for (int i = 0; i < Header.Length; i++)
            if (data[i] != Header[i]) throw new CryptographicException("无法识别存档加密格式。");
        byte[] keys = DeriveKeys(password, Slice(data, Header.Length, SaltSize));
        using (var hmac = new HMACSHA256(Slice(keys, 32, 32)))
        {
            byte[] expected = hmac.ComputeHash(data, 0, data.Length - TagSize);
            int difference = 0;
            for (int i = 0; i < TagSize; i++) difference |= expected[i] ^ data[data.Length - TagSize + i];
            if (difference != 0) throw new CryptographicException("存档校验失败，密码不正确或文件已损坏。");
        }
        using (var aes = Aes.Create())
        {
            aes.Key = Slice(keys, 0, 32);
            aes.IV = Slice(data, Header.Length + SaltSize, IvSize);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            int offset = Header.Length + SaltSize + IvSize;
            using (var decryptor = aes.CreateDecryptor())
                return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(data, offset, data.Length - offset - TagSize));
        }
    }

    private static byte[] DeriveKeys(string password, byte[] salt)
    {
        using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            return derive.GetBytes(64);
    }

    private static byte[] Slice(byte[] bytes, int offset, int length)
    {
        var result = new byte[length];
        Buffer.BlockCopy(bytes, offset, result, 0, length);
        return result;
    }
}
