using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using Message.Properties;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Security.Cryptography;
using Message;
using System.Net.NetworkInformation;
using System.Data.SQLite;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using Org.BouncyCastle.Asn1.X509;
using System.Reflection;
namespace Message
{
    public partial class FormProfile : Form
    {
        public string emailname { set; get; }
        public string username { set; get; }
        private Aes aes;
       
        
        private byte[] encryptedAesKey; // Lưu trữ khóa AES được mã hóa RSA
        
        // Lưu trữ khóa RSA của client (mỗi client có thể có một khóa RSA riêng)
        private static Dictionary<Socket, RSACryptoServiceProvider> clientRsaKeys = new Dictionary<Socket, RSACryptoServiceProvider>();
        public FormProfile()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            //aes = Aes.Create();


        }
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr one, int two, int three, int four);
        string constring = "Data Source=database.db;Version=3;";  // Chuỗi kết nối SQLite
        private void guna2CircleButton1_Click(object sender, EventArgs e)
        {
            FormLogin f1 = new FormLogin();
            this.Hide();
            f1.Show();

        }

        private void Form2_Load(object sender, EventArgs e)
        {
            label2.Text = emailname;
            byte[] getimage = new byte[0];
            SQLiteConnection con = new SQLiteConnection(constring);
            con.Open();
            string q = "SELECT * FROM Login WHERE email = '" + label2.Text + "'";
            SQLiteCommand cmd = new SQLiteCommand(q, con);
            SQLiteDataReader dataReader = cmd.ExecuteReader();
            dataReader.Read();
            if (dataReader.HasRows)
            {
                username = dataReader["username"]?.ToString();
                label2.Text = dataReader["email"]?.ToString();
                guna2TextBox1.Text = dataReader["username"]?.ToString();
                guna2TextBox2.Text = dataReader["email"]?.ToString();
                guna2TextBox3.Text = dataReader["password"]?.ToString();
                guna2TextBox4.Text = dataReader["username"]?.ToString();
                guna2TextBox5.Text = dataReader["username"]?.ToString();
                byte[] images = (byte[])dataReader["image"];
                if (images == null)
                {
                    guna2CirclePictureBox1.Image = null;
                    guna2CirclePictureBox2.Image = null;

                }
                else
                {
                    MemoryStream me = new MemoryStream(images);
                    guna2CirclePictureBox1.Image = Image.FromStream(me);
                    guna2CirclePictureBox2.Image = Image.FromStream(me);

                }
            }
            panel6.Visible = false;
            panel5.Visible = false;
            panel7.Visible = false;
            con.Close();
            lsvMessage1.View = View.Details;
            lsvMessage1.Columns.Add("User", lsvMessage1.Width / 2 - 5, HorizontalAlignment.Left);  // Cột tin nhắn người khác
            lsvMessage1.Columns.Add("Me", lsvMessage1.Width / 2 - 5, HorizontalAlignment.Right);  // Cột tin nhắn của bạn
            lsvMessage1.HeaderStyle = ColumnHeaderStyle.None;  // Ẩn tiêu đề
            lsvMessage1.Font = new Font("Times New Roman", 14, FontStyle.Regular); // Thay đổi font chữ
        }
        private bool check;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (check)
            {
                panel2.Width += 10;
                if (panel2.Size == panel2.MaximumSize)
                {
                    pictureBox1.Left = +230;
                    timer1.Stop();
                    check = false;
                    pictureBox1.Image = Resources.download__6_;
                }
            }
            else
            {
                panel2.Width -= 10;
                if (panel2.Size == panel2.MinimumSize)
                {
                    pictureBox1.Left = 23;
                    timer1.Stop();
                    check = true;
                    pictureBox1.Image = Resources.download__3_;
                }
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            if (panel5.Visible == false)
            {
                panel5.Visible = true;
            }
            else
            {
                panel5.Visible = false;
            }
        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel4_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(Handle, 0x112, 0xf012, 0);
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            if (panel6.Visible == false)
            {
                panel6.Visible = true;
            }
            else
            {
                panel6.Visible = false;
            }
        }



        IPEndPoint IP;
        Socket client;
        string rsaPublicKey; // Khóa RSA của server
        byte[] aesKey, aesIV; // Khóa AES
        void Connect()
        {
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try
            {
                client.Connect(IP);
            }
            catch
            {
                MessageBox.Show("Lỗi kết nối", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                // Nhận khóa RSA của server và mã hóa khóa AES gửi lại
                byte[] rsaKeyData = new byte[1024];
                int rsaKeyLength = client.Receive(rsaKeyData);
                rsaPublicKey = Encoding.UTF8.GetString(rsaKeyData, 0, rsaKeyLength);
                // Tạo khóa AES và gửi lại khóa AES đã mã hóa bằng RSA tới server
                using (Aes aes = Aes.Create())
                {
                    aesKey = aes.Key;
                    aesIV = aes.IV;
                    byte[] encryptedAesKey = EncryptionHelper.EncryptRSA(aesKey, rsaPublicKey);
                    client.Send(encryptedAesKey);
                }
            }
               
           
            catch (Exception ex) { 
                MessageBox.Show($"Lỗi trong quá trình trao đổi khóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                client.Close(); 
                return;
            }
            Thread listen = new Thread(Receive);
            listen.IsBackground = true;
            listen.Start();
        }
        
        void Close()
        {
            client.Close();
        }
        void Send()
        {
            if (!string.IsNullOrEmpty(txtMessage1.Text))
            {
                try
                {
                    // Kiểm tra kết nối trước khi gửi
                    if (client != null && client.Connected)
                    {
                        string fullMessage = $"{username}: {txtMessage1.Text}";
                        byte[] encryptedMessage = EncryptionHelper.EncryptAES(fullMessage, aesKey, aesIV);
                        /*string encryptedMessageBase64 = Convert.ToBase64String(encryptedMessage);
                        string messageToSend = "MSG:" + encryptedMessageBase64;
                        byte[] dataToSend = Encoding.UTF8.GetBytes(messageToSend);*/
                        //Gửi tin nhắn mã hóa
                        client.Send(encryptedMessage);
                        AddMessage(fullMessage); // Hiển thị tin nhắn đã gửi
                        txtMessage1.Clear();
                    }
                    else
                    {
                        MessageBox.Show("Chưa kết nối tới server.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi khi gửi tin nhắn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); }

            }
        }
        byte[] EncryptMessage(string message, byte[] key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.GenerateIV();
                using (var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        // Write IV to the stream for later decryption
                        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (var swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(message);
                            }
                            return msEncrypt.ToArray();
                        }
                    }
                }
            }
        }
        void SendEncryptedIP(string ip)
        {
            string encryptedIP = EncryptIP(ip, aesKey); // Mã hóa IP
            byte[] data = Encoding.UTF8.GetBytes("IP:" + encryptedIP); // Thêm tiền tố để server nhận biết
            client.Send(data); // Gửi đến server
            AddMessage("Đã gửi IP đã mã hóa: " + encryptedIP);
        }
        void Receive()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    int bytesRead = client.Receive(data);
                    if (bytesRead > 0)
                    {
                        byte[] receivedData = data.Take(bytesRead).ToArray();
                        string decryptedMessage = EncryptionHelper.DecryptAES(receivedData, aesKey, aesIV);
                        
                        
                        if (decryptedMessage.StartsWith("IMG:"))
                        {
                            string base64Image = decryptedMessage.Substring(4);
                            byte[] imageBytes = Convert.FromBase64String(base64Image);
                            ShowImage(imageBytes);  // Hiển thị ảnh
                            
                        }
                        else if (decryptedMessage.StartsWith("IP:")) // Kiểm tra nếu thông điệp là IP mã hóa
                        {
                            string encryptedIPBase64 = decryptedMessage.Substring(3);
                            byte[] encryptedIP = Convert.FromBase64String(encryptedIPBase64);

                            if (clientRsaKeys.ContainsKey(client))
                            {
                                // Nếu đã có khóa RSA của client, sử dụng nó để giải mã AES key và giải mã IP
                                RSACryptoServiceProvider rsaClient = clientRsaKeys[client];
                                byte[] decryptedAesKey = rsaClient.Decrypt(encryptedIP, false);
                                string decryptedIP = DecryptIP(encryptedIP, decryptedAesKey);
                                AddMessage($"Đã nhận IP: {decryptedIP}");
                            }
                            else
                            {
                                AddMessage("Không có khóa RSA của client để giải mã IP.");
                            }
                        }
                        else if (decryptedMessage.StartsWith("MSG:"))
                        {
                            /*string encryptedMessageBase64 = message.Substring(4);
                            byte[] encryptedData = Convert.FromBase64String(encryptedMessageBase64);

                            if (clientRsaKeys.ContainsKey(client))
                            {
                                // Nếu có khóa RSA của client, sử dụng nó để giải mã AES key và sau đó giải mã thông điệp
                                RSACryptoServiceProvider rsaClient = clientRsaKeys[client];
                                byte[] decryptedAesKey = rsaClient.Decrypt(encryptedData, false); // Giải mã AES key
                                string decryptedMessage = DecryptMessage(encryptedData, decryptedAesKey);
                                AddMessage(decryptedMessage); // Hiển thị thông điệp đã giải mã
                            }
                            else
                            {
                                AddMessage("Không có khóa RSA của client để giải mã tin nhắn.");
                            }*/
                            AddMessage("Server: " + decryptedMessage.Substring(4));
                        }
                    }
                }
            }
            catch
            {
                Close();
            }
        }
        void ShowImage(byte[] imageBytes)
        {
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                Image image = Image.FromStream(ms);
                Form imageForm = new Form
                {
                    Text = "Ảnh đã nhận",
                    Size = new Size(400, 400)
                };
                PictureBox pictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    Image = image,
                    SizeMode = PictureBoxSizeMode.Zoom
                };
                imageForm.Controls.Add(pictureBox);
                imageForm.ShowDialog();
            }
        }

        string DecryptMessage(byte[] cipherText, byte[] key)
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
        // Hàm mã hóa IP với AES
        public string EncryptIP(string ip, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length); // Ghi IV vào đầu stream
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(ip);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        // Hàm giải mã IP với AES
        public string DecryptIP(byte[] encryptedIP, byte[] key)
        {
            // Log the encrypted IP and its byte array
            Console.WriteLine("Cipher Text: " + BitConverter.ToString(encryptedIP));
            /*byte[] cipherText = Convert.FromBase64String(encryptedIP);
            // Log the encrypted IP and its byte array
            Console.WriteLine("Encrypted IP: " + encryptedIP);
            Console.WriteLine("Cipher Text: " + BitConverter.ToString(cipherText));*/

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                byte[] iv = encryptedIP.Take(16).ToArray();
                aesAlg.IV = iv;

                using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                using (var msDecrypt = new MemoryStream(encryptedIP.Skip(16).ToArray()))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    string decryptedIP = srDecrypt.ReadToEnd();
                    // Log the decrypted IP
                    Console.WriteLine("Decrypted IP: " + decryptedIP);
                    return decryptedIP;
                }
            }
        }

        private StringBuilder messages = new StringBuilder();
        void AddMessage(string message)
        {
            ListViewItem item;

            if (message.StartsWith($"{username}:")) // Tin nhắn của bạn
            {
                item = new ListViewItem(new[] { "", message });
                item.ForeColor = Color.White;
                item.Font = new Font("Times New Roman", 14, FontStyle.Regular); // Thay đổi font chữ
            }
            else // Tin nhắn từ người khác
            {
                item = new ListViewItem(new[] { message, "" });
                item.ForeColor = Color.White;
                item.Font = new Font("Times New Roman", 14, FontStyle.Regular); // Thay đổi font chữ
            }

            lsvMessage1.Items.Add(item);  // Thêm vào ListView
        }

        byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }
        object Deseriliaze(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }
       

        private void btnSend_Click_1(object sender, EventArgs e)
        {
            Send();
        }

        

        private void btnStart_Click(object sender, EventArgs e)
        {
            guna2TextBox5.Visible = true;
            panel7.Visible = true;
            Connect();
            
        }

        private void btnimage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] imageBytes = File.ReadAllBytes(openFileDialog.FileName);
                        SendImage(imageBytes);  // Gọi hàm gửi ảnh
                        AddMessage("Send an image");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi gửi ảnh: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        void SendImage(byte[] imageBytes)
        {
            try
            {
                // Chuyển đổi mảng byte ảnh thành chuỗi Base64
                string base64Image = Convert.ToBase64String(imageBytes);

                // Thêm prefix "IMG:" để nhận diện dữ liệu là ảnh
                string messageToSend = "IMG:" + base64Image;

                // Chuyển chuỗi thành mảng byte và gửi qua socket
                byte[] dataToSend = Encoding.UTF8.GetBytes(messageToSend);

                client.Send(dataToSend);  // Gửi dữ liệu ảnh qua socket
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi Socket", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi không xác định: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void emoji_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Kiểm tra xem emojiCB có lựa chọn nào không
            if (emoji.SelectedItem != null)
            {
                // Thêm emoji vào msgToSend
                txtMessage1.Text += emoji.SelectedItem.ToString();

                // Reset emojiCB về biểu tượng cảm xúc mặc định
                emoji.SelectedIndex = -1; // Hủy lựa chọn hiện tại (có thể thay bằng 0 nếu muốn giữ giá trị đầu tiên)
            }
        }
    }
}
// Tạo cặp khóa RSA cho mỗi client và server
public static class EncryptionHelper
{
    // Tạo cặp khóa RSA và xuất ra dưới dạng chuỗi XML
    public static RSACryptoServiceProvider GenerateRSAKeys(out string publicKey, out string privateKey)
    {
        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048)) { privateKey = rsa.ToXmlString(true); RSAParameters rsaParams = rsa.ExportParameters(false); string modulusBase64 = Convert.ToBase64String(rsaParams.Modulus); string exponentBase64 = Convert.ToBase64String(rsaParams.Exponent); publicKey = $@"{{ ""XmlPublicKey"": ""{rsa.ToXmlString(false)}"", ""Modulus"": ""{modulusBase64}"", ""Exponent"": ""{exponentBase64}"" }}"; return rsa; }
    }
    public static byte[] EncryptAES(string plainText, byte[] key, byte[] iv)
    {
        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (StreamWriter sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    return ms.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error encrypting data", ex);
        }
    }

    public static string DecryptAES(byte[] cipherText, byte[] key, byte[] iv)
    {
        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream(cipherText))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        catch (Exception ex) { throw new InvalidOperationException("Error decrypting data", ex); }
    }
    public static byte[] EncryptRSA(byte[] data, string publicKey)
    {
        try {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                // Remove any prefix from the publicKey string if present
                if (publicKey.StartsWith("RSA_PUBLIC_KEY:"))
                {
                    publicKey = publicKey.Replace("RSA_PUBLIC_KEY:", "").Trim();
                }
                rsa.FromXmlString(publicKey);
                return rsa.Encrypt(data, false);
            }
        }
        catch (Exception ex) { throw new InvalidOperationException("Error encrypting data with RSA", ex); }
    }

    public static byte[] DecryptRSA(byte[] data, string privateKey)
    {

        try
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);
                return rsa.Decrypt(data, false);
            }
        }
        catch (Exception ex) { throw new InvalidOperationException("Error decrypting data with RSA", ex); }
    }
}
