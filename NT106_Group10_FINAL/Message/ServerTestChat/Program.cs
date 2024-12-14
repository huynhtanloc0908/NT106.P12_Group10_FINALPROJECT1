using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ServerTestChat.Class1;

namespace ServerTest
{
    internal class Program
    {
        private static Aes aes = Aes.Create();
        private static Dictionary<Socket, byte[]> clientAesKeys = new Dictionary<Socket, byte[]>();
        private static IPEndPoint IP;
        private static Socket server;
        private static List<ClientInfo> clientList = new List<ClientInfo>();
        private static string rsaPublicKey;
        private static RSACryptoServiceProvider rsaProvider;
        static void Main(string[] args)
        {
            Console.WriteLine("Server starting...");
            Connect();
            Console.ReadLine(); // giữ console mở
        }

         
        static void Connect()
        {
            // Tạo cặp khóa RSA cho server
            rsaProvider = new RSACryptoServiceProvider(2048);
            rsaPublicKey = rsaProvider.ToXmlString(false);
            IP = new IPEndPoint(IPAddress.Any, 11000);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            server.Bind(IP);
            
            Thread listen = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        Socket clientSocket = server.Accept();
                        ClientInfo client = new ClientInfo { Socket = clientSocket };
                        clientList.Add(client);
                        // Gửi khóa RSA công khai của server đến client
                        byte[] publicKeyBytes = Encoding.UTF8.GetBytes(rsaPublicKey);
                        clientSocket.Send(publicKeyBytes);
                        // Nhận khóa AES từ client
                        Thread receiveAESKey = new Thread(() => ReceiveAESKey(client));
                        receiveAESKey.IsBackground = true;
                        receiveAESKey.Start();
                       


                    }
                }
                catch
                {
                    IP = new IPEndPoint(IPAddress.Any, 11000);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }
            });
            listen.IsBackground = true;
            listen.Start();
        }
        static void ReceiveAESKey(ClientInfo client)
        {
            try
            {
                byte[] buffer = new byte[256];
                int bytesReceived = client.Socket.Receive(buffer);
                byte[] encryptedAESKey = new byte[bytesReceived];
                Array.Copy(buffer, encryptedAESKey, bytesReceived);

                // Giải mã khóa AES sử dụng RSA
                byte[] aesKey = rsaProvider.Decrypt(encryptedAESKey, false);
                client.AESKey = aesKey;

                Thread receive = new Thread(() => Receive(client));
                receive.IsBackground = true;
                receive.Start();
            }
            catch (Exception ex)
            {
                
                clientList.Remove(client);
            }
        }
        static void RestartServer()
        {
            Console.WriteLine("Restarting server...");
            server.Close();
            Connect();
        }
        static void Send(Socket client, byte[] encryptedMessage)
        {
            if (client != null && client.Connected)
            {
                client.Send(encryptedMessage);
            }
        }
        static void Receive(ClientInfo client)
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    int bytesRead = client.Socket.Receive(data);

                    if (bytesRead > 0)
                    {
                        byte[] encryptedData = data.Take(bytesRead).ToArray();
                        string message = DecryptAES(encryptedData, client.AESKey);

                        if (message.StartsWith("MSG:"))
                        {
                            AddMessage(message.Substring(4));
                            foreach (ClientInfo item in clientList)
                            {
                                if (item.Socket != client.Socket)
                                {
                                    byte[] encryptedMessage = EncryptAES(message, item.AESKey);
                                    Send(item.Socket, encryptedMessage);
                                }
                            }
                        }

                    }
                }
            }
            catch
            {
                clientList.Remove(client);
                client.Socket.Close();
            }
        }
        
        static byte[] EncryptAES(string plainText, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return ms.ToArray();
                }
            }
        }
        static string DecryptAES(byte[] cipherText, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    byte[] iv = new byte[16];
                    ms.Read(iv, 0, iv.Length);
                    aes.IV = iv;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

       
        

        

        static byte[] EncryptMessage(string message, byte[] key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.GenerateIV();
                using (var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
                using (var ms = new MemoryStream())
                {
                    ms.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(message);
                    }
                    return ms.ToArray();
                }
            }
        }
        static string DecryptMessage(byte[] cipherText, byte[] key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;

                // The IV is the first 16 bytes of the cipher text
                byte[] iv = cipherText.Take(16).ToArray();
                aesAlg.IV = iv;

                using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                {
                    using (var msDecrypt = new MemoryStream(cipherText.Skip(16).ToArray()))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
        static string DecryptIP(byte[] encryptedIP, byte[] key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                byte[] iv = encryptedIP.Take(16).ToArray();
                aesAlg.IV = iv;

                using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                using (var ms = new MemoryStream(encryptedIP.Skip(16).ToArray()))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        static void AddMessage(string message)
        {
            Console.WriteLine(message);
        }
        // File sending function
        

        private static bool IsImageFile(string fileName)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
            return imageExtensions.Contains(Path.GetExtension(fileName).ToLower());
        }
    }
}

