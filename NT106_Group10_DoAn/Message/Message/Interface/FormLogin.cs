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
using System.Runtime.InteropServices;
using System.Data.SQLite;
namespace Message
{
    public partial class FormLogin : Form
    {
        public FormLogin()
        {
            InitializeComponent();
        }
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr one, int two, int three, int four);
        Color Btn = Color.SpringGreen;
        Color bb = Color.Gray;
        string constring = "Data Source=database.db;Version=3;";
        private void guna2Button1_Click(object sender, EventArgs e)
        {
            panel1.BringToFront();
            ButtonRegister.FillColor = Btn;
            ButtonLogin.FillColor = Btn;
            panel4.BackColor = Btn;
            panel3.BackColor = bb;

        }

        private void ButtonRegister_Click(object sender, EventArgs e)
        {
            panel2.BringToFront();
            ButtonLogin.FillColor = Btn;
            ButtonRegister.FillColor = Btn;
            panel4.BackColor = bb;
            panel3.BackColor = Btn;
            panel5.Visible = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ButtonLogin.PerformClick();
        }

        private void guna2Button4_Click(object sender, EventArgs e)
        {
            if (guna2CirclePictureBox1 == null)
            {
                MessageBox.Show("Vui lòng chọn ảnh");
                return;
            }
           
                if (string.IsNullOrEmpty(usernameText.Text.Trim()))
                {
                    errorProvider1.SetError(usernameText, "username không được để trống");
                    return;
                }
                else
                {
                    errorProvider1.SetError(usernameText, string.Empty);
                }
                if (string.IsNullOrEmpty(emailText.Text.Trim()))
                {
                    errorProvider1.SetError(emailText, "email không được để trống");
                    return;
                }
                else
                {
                    errorProvider1.SetError(emailText, string.Empty);
                }
                if (string.IsNullOrEmpty(passwordText.Text.Trim()))
                {
                    errorProvider1.SetError(passwordText, "password không được để trống");
                    return;
                }
                else
                {
                    errorProvider1.SetError(passwordText, string.Empty);
                }
                if (string.IsNullOrEmpty(confirmText.Text.Trim()))
                {
                    errorProvider1.SetError(confirmText, "confirmpassword không được để trống");
                    return;
                }
                else
                {
                    errorProvider1.SetError(confirmText, string.Empty);
                }
                if (passwordText.Text != confirmText.Text)
                {
                    MessageBox.Show("password với confirm password không trùng nhau , vui lòng nhập lại");
                    return;
                }
            try
            {
                SQLiteConnection con = new SQLiteConnection(constring);
                string q = "insert into Login(username, email, password, confirmpass, image)" + "values(@username, @email, @password, @confirmpass, @image)";
                SQLiteCommand cmd = new SQLiteCommand(q, con);
                MemoryStream me = new MemoryStream();
                guna2CirclePictureBox1.Image.Save(me, guna2CirclePictureBox1.Image.RawFormat);
                cmd.Parameters.AddWithValue("@username", usernameText.Text);
                cmd.Parameters.AddWithValue("@email", emailText.Text);
                cmd.Parameters.AddWithValue("@password", passwordText.Text);
                cmd.Parameters.AddWithValue("@confirmpass", confirmText.Text);
                cmd.Parameters.AddWithValue("@image", me.ToArray());
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                MessageBox.Show("Đăng kí thành công.....");
                usernameText.Clear();
                emailText.Clear();
                passwordText.Clear();
                confirmText.Clear();
                guna2CirclePictureBox1.Image = null;
            }
            catch (Exception ex) 
            {
                MessageBox.Show("Có lỗi xảy ra: " + ex.Message);

            }

        }

        private void guna2CirclePictureBox1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "chọn ảnh(*.Jpg; *.pnq; *.Gif|*.Jpg; *.pnq; *.Gif";
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                guna2CirclePictureBox1.Image=Image.FromFile(openFileDialog1.FileName);
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(emailloginText.Text.Trim()))
            {
                errorProvider1.SetError(emailloginText, "email không được để trống");
                return;
            }
            else
            {
                errorProvider1.SetError(emailloginText, string.Empty);
            }
            if (string.IsNullOrEmpty(passwordloginText.Text.Trim()))
            {
                errorProvider1.SetError(passwordloginText, "password không được để trống");
                return;
            }
            else
            {
                errorProvider1.SetError(passwordloginText, string.Empty);
            }
            SQLiteConnection con = new SQLiteConnection(constring);
            con.Open();
            string q = "Select * from login WHERE email = '" + emailloginText.Text + "'AND password = '" + passwordloginText.Text + "'";
            SQLiteCommand cmd = new SQLiteCommand(q, con);
            SQLiteDataReader dataReader;
            dataReader = cmd.ExecuteReader();
            if (dataReader.HasRows == true)
            {
                panel5.BringToFront();
                panel5.Visible = true;
                timer1.Start();

            }
            else
            {
                MessageBox.Show("Vui lòng kiểm tra lại email và mật khẩu");
            }
            con.Close();
        }

        

        private void passwordloginText_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel7_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(Handle, 0x112, 0xf012, 0);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (guna2CircleProgressBar1.Value < 100)
            {
                guna2CircleProgressBar1.Value += 2;
            }
            else
            {
                timer1.Stop();
                FormProfile f3 = new FormProfile();
                FormProfile f2=new FormProfile();
                f2.emailname=emailloginText.Text;
                f3.username=usernameText.Text;
                this.Hide();
                f2.Show();
            }
        }

        
    }
}
