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
        private static IPEndPoint IP;
        private static Socket server;
        private static List<Socket> clientList = new List<Socket>();
        static void Main(string[] args)
        {
            Console.WriteLine("Server starting...");
            Connect();
            Console.ReadLine(); // giữ console mở
        }



        static void Connect()
        {

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
                        Socket client = server.Accept();
                        clientList.Add(client);
                        AddMessage("Client connected "); /*+ client.RemoteEndPoint.ToString())*/
                        aes.GenerateKey();
                        byte[] keyMessage = aes.Key;
                        clientAesKeys[client] = keyMessage; // Store AES key for the client
                        client.Send(keyMessage); // Send AES key to the client
                        // Create a separate thread to receive data from the client
                        Thread receive = new Thread(() => Receive(client));
                        receive.IsBackground = true;
                        receive.Start();


                    }
                }
                catch
                {
                    IP = new IPEndPoint(IPAddress.Any, 1997);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }
            });
            listen.IsBackground = true;
            listen.Start();
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
                // Extract the message content without decrypting it
                string encryptedMessageBase64 = message.Substring(4);
                string encryptedClientIPBase64 = Convert.ToBase64String(EncryptMessage(client.RemoteEndPoint.ToString(), clientAesKeys[client]));
                AddMessage("Client IP: " + encryptedClientIPBase64 + ") sent message :  " + encryptedMessageBase64);

                // Send the encrypted message to all other clients
                foreach (Socket otherClient in clientList)
                {
                    if (otherClient != client) // Do not send back to the sender
                    {
                        if (clientAesKeys.ContainsKey(otherClient))
                        {
                            byte[] encryptedMessage = EncryptMessage(DecryptMessage(Convert.FromBase64String(encryptedMessageBase64), clientAesKeys[client]), clientAesKeys[otherClient]);
                            string messageToSend = "MSG:" + Convert.ToBase64String(encryptedMessage);
                            byte[] dataToSend = Encoding.UTF8.GetBytes(messageToSend);
                            otherClient.Send(dataToSend);
                        }
                        else
                        {
                            AddMessage("No AES key for client: " + otherClient.RemoteEndPoint.ToString());
                        }
                    }
                }
            }
            else if (message.StartsWith("IMG:"))
            {
                string base64Image = message.Substring(4);
                byte[] imageBytes = Convert.FromBase64String(base64Image);
                string encryptedClientIPBase64 = Convert.ToBase64String(EncryptMessage(client.RemoteEndPoint.ToString(), clientAesKeys[client]));
                AddMessage("Client " + encryptedClientIPBase64 + " sent an image.");

                // Send the image to all other clients
                foreach (Socket otherClient in clientList)
                {
                    if (otherClient != client) // Do not send back to the sender
                    {
                        string messageToSend = "IMG:" + base64Image;
                        byte[] dataToSend = Encoding.UTF8.GetBytes(messageToSend);
                        otherClient.Send(dataToSend);
                        
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
        static async Task SendFileAsync(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            byte[] fileData = File.ReadAllBytes(filePath);

            string filePrefix = IsImageFile(fileName) ? "IMG:" : "FILE:";
            string fileHeaderMessage = filePrefix + fileName;
            byte[] fileHeader = Encoding.UTF8.GetBytes(fileHeaderMessage);

            foreach (Socket client in clientList)
            {
                if (client != null && client.Connected)
                {
                    client.Send(fileHeader);
                    await Task.Run(() => client.Send(fileData));
                }
            }

            AddMessage($"File sent: {fileName}");
        }

        private static bool IsImageFile(string fileName)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" };
            return imageExtensions.Contains(Path.GetExtension(fileName).ToLower());
        }
    }
}
