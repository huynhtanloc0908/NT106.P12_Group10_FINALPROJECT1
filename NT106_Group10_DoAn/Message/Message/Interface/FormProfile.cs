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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
namespace Message
{
    public partial class FormProfile : Form
    {
        public string emailname {set;get ;}
        public string username { set;get ;}
        private Aes aes;
        private byte[] aesKey; // Store the AES key received from the server

        public FormProfile()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            aes = Aes.Create();
    
        }
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr one, int two, int three, int four);
        string constring = "Data Source=database.db;Version=3;";  // Chuỗi kết nối SQLite
        private void guna2CircleButton1_Click(object sender, EventArgs e)
        {
            FormLogin f1=new FormLogin();
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
                if(panel2.Size== panel2.MaximumSize)
                {
                    pictureBox1.Left = +230;
                    timer1.Stop();
                    check=false;
                    pictureBox1.Image = Resources.download__6_;
                }
            }
            else
            {
                panel2.Width -= 10;
                if(panel2.Size== panel2.MinimumSize)
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
            aesKey = new byte[32]; // AES key size is 256 bits (32 bytes)
            client.Receive(aesKey); // Receive AES key from the server


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
                string fullMessage = $"{username}: {txtMessage1.Text}";
                byte[] encryptedMessage = EncryptMessage(fullMessage, aesKey);
                string encryptedMessageBase64 = Convert.ToBase64String(encryptedMessage);
                string messageToSend = "MSG:" + encryptedMessageBase64;
                byte[] dataToSend = Encoding.UTF8.GetBytes(messageToSend);

                client.Send(dataToSend);
                AddMessage(fullMessage); // Hiển thị tin nhắn đã gửi
                txtMessage1.Clear();

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
                        string message = Encoding.UTF8.GetString(data.Take(bytesRead).ToArray());
                        if (message.StartsWith("IMG:"))
                        {
                            string base64Image = message.Substring(4);
                            byte[] imageBytes = Convert.FromBase64String(base64Image);
                            ShowImage(imageBytes);  // Hiển thị ảnh
                            
                        }
                        else if (message.StartsWith("IP:")) // Kiểm tra nếu thông điệp là IP mã hóa
                        {
                            string encryptedIP = message.Substring(3); // Loại bỏ tiền tố 'IP:'
                            string decryptedIP = DecryptIP(encryptedIP, aesKey); // Giải mã IP
                            AddMessage($"Đã nhận IP: {decryptedIP}"); // Hiển thị IP đã giải mã
                        }
                        else if (message.StartsWith("MSG:"))
                        {
                            string encryptedMessageBase64 = message.Substring(4);
                            byte[] encryptedData = Convert.FromBase64String(encryptedMessageBase64);
                            // Decrypt message using client's AES key
                            string decryptedMessage = DecryptMessage(encryptedData, aesKey);
                            AddMessage(decryptedMessage); // Display decrypted message

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
        public string DecryptIP(string encryptedIP, byte[] key)
        {
            byte[] cipherText = Convert.FromBase64String(encryptedIP);
            // Log the encrypted IP and its byte array
            Console.WriteLine("Encrypted IP: " + encryptedIP);
            Console.WriteLine("Cipher Text: " + BitConverter.ToString(cipherText));

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                byte[] iv = cipherText.Take(16).ToArray();
                aesAlg.IV = iv;

                using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                using (var msDecrypt = new MemoryStream(cipherText.Skip(16).ToArray()))
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
