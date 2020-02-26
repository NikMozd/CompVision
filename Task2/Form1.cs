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
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, bmpData.Scan0, bytes.Length);
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

        private byte[] Otsu(byte[] bytes)
        {
            var hist = GetHistogram(bytes);
            var threshold = GetOtsuThreshold(hist);
            var bytesNew = bytes.Select(b => (byte)(b >= threshold ? 255 : 0)).ToArray();
            return bytesNew;
        }

        private double[] GetHistogram(byte[] bytes)
        {
            var hist = new double[256];
            foreach (var b in bytes)
                hist[b]++;

            return hist.Select(h => h / bytes.Length).ToArray();
        }

        private byte GetOtsuThreshold(double[] hist)
        {
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
            return (byte)threshold;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var bytes = GetImageBytesGray();
            var bytesNew = Otsu(bytes);
            SetImageBytes(bytesNew);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox3.Text, out var segmentCountWidth) || segmentCountWidth < 1)
                throw new ArgumentException("Неправильное число блоков по-горизонтали");
            if (!int.TryParse(textBox4.Text, out var segmentCountHeight) || segmentCountHeight < 1)
                throw new ArgumentException("Неправильное число блоков по-вертикали");

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

            var bytesArrays = new byte[segmentCountHeight, segmentCountWidth][];
            for (int i = 0; i < segmentCountHeight; ++i)
                for (int j = 0; j < segmentCountWidth; ++j)
                {
                    var segment = tmp.Where(tp => tp.Row == i && tp.Column == j).Select(tp => tp.Value).ToArray();
                    var segmentNew = Otsu(segment);
                    bytesArrays[i, j] = segmentNew;
                }

            var bytesNew = new List<byte>(bytes.Length);
            for (int i = 0; i < segmentCountHeight * height; ++i)
                for (int j = 0; j < segmentCountWidth; ++j)
                    bytesNew.AddRange(bytesArrays[i / height, j].Skip((i % height) * width).Take(width));

            SetImageBytes(bytesNew.ToArray());
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox5.Text, out var depth) || depth < 1)
                throw new ArgumentException("Неправильная глубина");

            var bytes = GetImageBytesGray();
            var hist = GetHistogram(bytes);
            var thresholds = new List<byte> { 0, 255 };

            for (int counter = 0; counter < depth; ++counter)
            {
                var thresholdsNew = new List<byte>();
                var fst = thresholds.First();
                foreach (var snd in thresholds.Skip(1))
                {
                    var histTemp = GetHistogram(bytes.Where(b => fst <= b && b <= snd).ToArray());
                    var thresholdTemp = GetOtsuThreshold(histTemp);
                    if (thresholdTemp != fst && thresholdTemp != snd)
                        thresholdsNew.Add(thresholdTemp);
                    fst = snd;
                }

                thresholds.AddRange(thresholdsNew);
                thresholds.Sort();
            }

            var bytesNew = bytes.Select(b => (byte)(thresholds.FindLastIndex(t => t <= b) % 2 == 0 ? 0 : 255)).ToArray();
            SetImageBytes(bytesNew);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox6.Text, out var levels) || levels < 2)
                throw new ArgumentException("Неправильное число уровней");

            var bytes = GetImageBytesGray();
            var step = 256 / levels;
            var bytesNew = bytes.Select(b => (byte)Math.Min(255, step * (b / step + 1))).ToArray();
            SetImageBytes(bytesNew);
        }
    }
}
