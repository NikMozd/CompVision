using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Task0._2
{
	public partial class Form1 : Form
	{

		double[] countRed, countGreen, countBlue;
		public Form1()
		{
			InitializeComponent();
			pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
        }

		byte[] rgbValuesRed, rgbValuesGreen, rgbValuesBlue;
		private void Form1_Load(object sender, EventArgs e)
		{

		}

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            label1.Text = openFileDialog1.FileName;
        }

        private void button2_Click(object sender, EventArgs e)
		{
			pictureBox1.Image = Bitmap.FromFile(label1.Text);
			Bitmap bmp1 = pictureBox1.Image as Bitmap;

			var l = bmp1.Size.Width;
			var h = bmp1.Size.Height;

			rgbValuesRed = new byte[l * h];
			rgbValuesGreen = new byte[l * h];
			rgbValuesBlue = new byte[l * h];
			for (int i = 0; i < l; ++i)
				for (int j = 0; j < h; ++j)
				{
					var clr = bmp1.GetPixel(i, j);
					rgbValuesRed[i * h + j] = clr.R;
					rgbValuesGreen[i * h + j] = clr.G;
					rgbValuesBlue[i * h + j] = clr.B;
				}

			countRed = new double[256];
            countGreen = new double[256];
            countBlue = new double[256];
            countColors(l * h);


			pictureBox1.Refresh();
            this.pictureBox2.Visible = true;
            pictureBox2.Refresh();
			
            this.pictureBox4.Visible = true;
            pictureBox4.Refresh();
			
            this.pictureBox6.Visible = true;
            pictureBox6.Refresh();
        }

        private void countColors(int length)
		{
			for (int i = 0; i < length; ++i)
            {
                countBlue[rgbValuesBlue[i]]++;
                countGreen[rgbValuesGreen[i]]++;
                countRed[rgbValuesRed[i]]++;
            }

			for (int i = 0; i < 256; ++i)
			{
				countBlue[i] /= length;
				countGreen[i] /= length;
				countRed[i] /= length;
			}
		}

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
			double max = countRed.Max();
            System.Drawing.Graphics g;
            g = e.Graphics;
            int h = this.pictureBox2.Height;
            Pen myPen = new Pen(System.Drawing.Color.Red, 1);
            for (int i = 0; i < 256; ++i)
                g.DrawLine(myPen, i, h, i, (int)(h * (max - countRed[i]) / max));
        }

        private void pictureBox4_Paint(object sender, PaintEventArgs e)
        {
			double max = countGreen.Max();
            System.Drawing.Graphics g;
            g = e.Graphics;
            int h = this.pictureBox4.Height;
            Pen myPen = new Pen(System.Drawing.Color.Green, 1);
            for (int i = 0; i < 256; ++i)
                g.DrawLine(myPen, i, h, i, (int)(h * (max - countGreen[i]) / max));
        }

        private void pictureBox6_Paint(object sender, PaintEventArgs e)
        {
			double max = countBlue.Max();
            System.Drawing.Graphics g;
            g = e.Graphics;
            int h = this.pictureBox6.Height;
            Pen myPen = new Pen(System.Drawing.Color.Blue, 1);
            for (int i = 0; i < 256; ++i)
                g.DrawLine(myPen, i, h, i, (int)(h * (max - countBlue[i]) / max));
        }
    }
}
