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

namespace ServerTest
{
    internal class Program
    {
        private static Aes aes = Aes.Create();
        private static Dictionary<Socket, byte[]> clientAesKeys = new Dictionary<Socket, byte[]>();
        private static List<Socket> clientList = new List<Socket>();
        private static Socket server;
        static void Main(string[] args)
        {
            Console.WriteLine("Server starting...");
            Connect();
        }
        


        static void Connect()
        {
            IPEndPoint IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1997);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            server.Bind(IP);

            Thread listenThread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        clientList.Add(client);
                        AddMessage("Client connected: " + client.RemoteEndPoint);

                        aes.GenerateKey();
                        byte[] key = aes.Key;
                        clientAesKeys[client] = key;
                        client.Send(key); // Send AES key to the client

                        Thread receiveThread = new Thread(() => Receive(client));
                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    RestartServer();
                }
            });
            listenThread.IsBackground = true;
            listenThread.Start();
        }

        static void RestartServer()
        {
            Console.WriteLine("Restarting server...");
            server.Close();
            Connect();
        }

        static void Receive(Socket client)
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    int bytesRead = client.Receive(data);

                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(data.Take(bytesRead).ToArray());
                        HandleMessage(client, message);
                    }
                    else
                    {
                        AddMessage("No data received.");
                    }
                }
            }
            catch (Exception ex)
            {
                AddMessage("Client disconnected: " + client.RemoteEndPoint);
                clientList.Remove(client);
                client.Close();
                clientAesKeys.Remove(client);
            }
        }

        static void HandleMessage(Socket client, string message)
        {
            if (message.StartsWith("IP:"))
            {
                string encryptedIPBase64 = message.Substring(3);
                byte[] encryptedIP = Convert.FromBase64String(encryptedIPBase64);

                if (clientAesKeys.ContainsKey(client))
                {
                    string decryptedIP = DecryptIP(encryptedIP, clientAesKeys[client]);
                    AddMessage($"Client sent decrypted IP: {decryptedIP}");
                }
                else
                {
                    AddMessage("No AES key available to decrypt IP.");
                }
            }
            else if (message.StartsWith("MSG:"))
            {
                string encryptedMessageBase64 = message.Substring(4);
                AddMessage($"Received encrypted message: {encryptedMessageBase64}");

                BroadcastMessage(client, encryptedMessageBase64);
            }
            else if (message.StartsWith("IMG:"))
            {
                string base64Image = message.Substring(4);
                AddMessage("Received an image.");

                BroadcastMessage(client, message);
            }
        }

        static void BroadcastMessage(Socket sender, string message)
        {
            foreach (Socket client in clientList)
            {
                if (client != sender && client.Connected)
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    client.Send(data);
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
    }
}
