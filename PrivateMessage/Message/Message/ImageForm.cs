using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Message
{
    public partial class ImageForm : Form
    {
        public ImageForm(byte[] imageBytes)
        {
            InitializeComponent();
            DisplayImage(imageBytes);
        }
        private void DisplayImage(byte[] imageBytes)
        {
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                Image receivedImage = Image.FromStream(ms);
                pictureBox1.Image = receivedImage;
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                    saveFileDialog.Title = "Save Image";
                    saveFileDialog.FileName = "downloaded_image";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        pictureBox1.Image.Save(saveFileDialog.FileName);
                        MessageBox.Show("Image downloaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                MessageBox.Show("No image to download!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }

}
