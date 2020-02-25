using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Task2
{
    public partial class Form1 : Form
    {
        private System.Drawing.Imaging.BitmapData bmpData;
        private int bytesCount;

        private const int segmentCountWidth = 3;
        private const int segmentCountHeight = 2;

        public Form1()
        {
            InitializeComponent();
        }

        private byte[] GetImageBytes()
        {
            if (pictureBox1.Image == null)
                throw new Exception("Пустое исходное изображение");

            pictureBox2.Image = pictureBox1.Image.Clone() as Image;
            var bmp = (Bitmap)pictureBox2.Image;
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
            bytesCount = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] res = new byte[bytesCount];
            System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, res, 0, bytesCount);
            return res;
        }

        private byte[] ColorBytesToGrayShades(byte[] bytes)
        {
            if (radioButton1.Checked)
                return bytes
                .Select((b, i) => new { B = b, I = i })
                .GroupBy(tmp => tmp.I / 3, tmp => tmp.B)
                .Select(bt => (byte)(bt.Select(b => (int)b).Sum() / 3))
                .SelectMany(b => new[] { b, b, b })
                .ToArray();
            if (radioButton2.Checked)
                return bytes
                .Select((b, i) => new { B = b, I = i })
                .GroupBy(tmp => tmp.I / 3, tmp => tmp.B)
                .Select(bt => bt.ToArray())
                .Select(bt => (byte)(0.3 * bt[2] + 0.59 * bt[1] + 0.11 * bt[0]))
                .SelectMany(b => new[] { b, b, b })
                .ToArray();
            throw new ArgumentException("Нужно выбрать метод перевода в оттенки серого");
        }

        private byte[] GetImageBytesGray() => ColorBytesToGrayShades(GetImageBytes());

        private void SetImageBytes(byte[] bytes)
        {
            var bmp = (Bitmap)pictureBox2.Image;
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, bmpData.Scan0, bytesCount);
            bmp.UnlockBits(bmpData);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var diag = new OpenFileDialog())
            {
                if (diag.ShowDialog() == DialogResult.OK)
                    pictureBox1.Image = new Bitmap(diag.FileName);
            }

            var bytes = GetImageBytes();
            var bytesNew = ColorBytesToGrayShades(bytes);
            SetImageBytes(bytesNew);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var bytes = GetImageBytes();
            var bytesNew = bytes
                .Select((b, i) => new { B = b, I = i })
                .GroupBy(tmp => tmp.I / 3, tmp => tmp.B)
                .Select(bt => (byte)(bt.Select(b => (int)b).Sum() / 3))
                .SelectMany(b => new[] { b, b, b })
                .ToArray();
            SetImageBytes(bytesNew);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var bytes = GetImageBytes();
            var bytesNew = bytes
                .Select((b, i) => new { B = b, I = i })
                .GroupBy(tmp => tmp.I / 3, tmp => tmp.B)
                .Select(bt => bt.ToArray())
                .Select(bt => (byte)(0.3 * bt[2] + 0.59 * bt[1] + 0.11 * bt[0]))
                .SelectMany(b => new[] { b, b, b })
                .ToArray();
            SetImageBytes(bytesNew);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!byte.TryParse(textBox1.Text, out var t1))
                throw new ArgumentException("Некорректный ввод порога 1");
            var bytes = GetImageBytesGray();
            var bytesNew = bytes.Select(b => (byte)(b >= t1 ? 255 : 0)).ToArray();
            SetImageBytes(bytesNew);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!byte.TryParse(textBox1.Text, out var t1))
                throw new ArgumentException("Некорректный ввод порога 1");
            var bytes = GetImageBytesGray();
            var bytesNew = bytes.Select(b => (byte)(b >= t1 ? 0 : 255)).ToArray();
            SetImageBytes(bytesNew);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (!byte.TryParse(textBox1.Text, out var t1))
                throw new ArgumentException("Некорректный ввод порога 1");
            if (!byte.TryParse(textBox2.Text, out var t2))
                throw new ArgumentException("Некорректный ввод порога 2");
            if (t1 > t2)
                throw new ArgumentException("Порог 1 не должен быть больше порога 2");
            var bytes = GetImageBytesGray();
            var bytesNew = bytes.Select(b => (byte)(t1 <= b && b <= t2 ? 255 : 0)).ToArray();
            SetImageBytes(bytesNew);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var bytes = GetImageBytesGray();
            var hist = new double[256];
            foreach (var b in bytes)
                hist[b]++;
            
            hist = hist.Select(h => h / bytes.Length).ToArray();
            
            var psum = new double[256];
            var msum = new double[256];
            psum[0] = hist[0];
            for (int i = 1; i < 256; ++i)
            {
                psum[i] = psum[i - 1] + hist[i];
                msum[i] = msum[i - 1] + i * hist[i];
            }

            var mg = msum.Last();
            var sigma = new double[256];
            for (int i = 0; i < 256; ++i)
            {
                sigma[i] = Math.Pow(mg * psum[i] - msum[i], 2) / (psum[i] * (1 - psum[i]));
                if (double.IsNaN(sigma[i]))
                    sigma[i] = 0;
            }

            var threshold = sigma.ToList().IndexOf(sigma.Max());
            var bytesNew = bytes.Select(b => (byte)(b >= threshold ? 255 : 0)).ToArray();
            SetImageBytes(bytesNew);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var bytes = GetImageBytesGray();
            List<byte>[] segments = new List<byte>[6];
            for (int i = 0; i < 6; ++i)
                segments[i] = new List<byte>();

            var width = bmpData.Stride / segmentCountWidth;
            var height = pictureBox2.Image.Height / segmentCountHeight;
            var tmp = bytes.Select((b, i) => new 
            { 
                Row = (i / bmpData.Stride) / height, 
                Column = (i % bmpData.Stride) / width, 
                Value = b
            });

            // ?????
        }
    }
}
