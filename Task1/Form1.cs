using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Task1
{
    public partial class Form1 : Form
    {
        private System.Drawing.Imaging.BitmapData bmpData;
        private int bytesCount;

        public Form1()
        {
            InitializeComponent();
            pictureBox3.Image = new Bitmap(pictureBox3.Width + 1, pictureBox3.Height + 1);
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

        private void SetImageBytes(byte[] bytes)
        {
            var bmp = (Bitmap)pictureBox2.Image;
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, bmpData.Scan0, bytesCount);
            bmp.UnlockBits(bmpData);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            using (var diag = new OpenFileDialog())
            {
                if (diag.ShowDialog() == DialogResult.OK)
                    pictureBox1.Image = new Bitmap(diag.FileName);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var bytes = GetImageBytes();
            var avgR = bytes.Where((b, i) => i % 3 == 2).Select(b => (double)b).Sum() / (bytes.Length / 3);
            var avgG = bytes.Where((b, i) => i % 3 == 1).Select(b => (double)b).Sum() / (bytes.Length / 3);
            var avgB = bytes.Where((b, i) => i % 3 == 0).Select(b => (double)b).Sum() / (bytes.Length / 3);
            var avg = (avgR + avgG + avgB) / 3;

            var coefs = new[] { avg / avgB, avg / avgG, avg / avgR };
            var bytesNew = bytes.Select((b, i) => (byte)Math.Min(255, (int)(b * coefs[i % 3]))).ToArray();
            SetImageBytes(bytesNew);
            pictureBox2.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == string.Empty
                || textBox2.Text == string.Empty
                || textBox3.Text == string.Empty
                || textBox4.Text == string.Empty
                || textBox5.Text == string.Empty)
                throw new Exception("Не заполнены координаты и/или цвет");
            var x = int.Parse(textBox1.Text);
            var y = int.Parse(textBox2.Text);
            var r = byte.Parse(textBox3.Text);
            var g = byte.Parse(textBox4.Text);
            var b = byte.Parse(textBox5.Text);

            var clrOld = (pictureBox1.Image as Bitmap).GetPixel(x, y);
            var bytes = GetImageBytes();
            var coefs = new[] { (double)b / clrOld.B, (double)g / clrOld.G, (double)r / clrOld.R };
            var bytesNew = bytes.Select((bt, i) => (byte)Math.Min(255, (int)(bt * coefs[i % 3]))).ToArray();
            SetImageBytes(bytesNew);
            pictureBox2.Refresh();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            using (var colorDiag = new ColorDialog())
            {
                if (colorDiag.ShowDialog() == DialogResult.OK)
                {
                    textBox3.Text = colorDiag.Color.R.ToString();
                    textBox4.Text = colorDiag.Color.G.ToString();
                    textBox5.Text = colorDiag.Color.B.ToString();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (textBox6.Text == string.Empty && listBox1.SelectedIndex == 3)
                throw new Exception("Не заполнен параметр");

            Func<byte, byte> convert;
            switch (listBox1.SelectedIndex)
            {
                case 0:
                    convert = x => (byte)(255 - x);
                    break;
                case 1:
                    convert = x => (byte)(Math.Log(1 + x) / Math.Log(256) * 255);
                    break;
                case 2:
                    convert = x => (byte)((Math.Exp((double)x / 255) - 1) / (Math.E - 1) * 255);
                    break;
                case 3:
                    var param = double.Parse(textBox6.Text);
                    convert = x => (byte)(Math.Pow(x, param) / Math.Pow(255, param) * 255);
                    break;
                default:
                    throw new Exception("Нереализованный вид преобразования");
            }

            var bytes = GetImageBytes();
            var bytesNew = bytes.Select(b => convert(b)).ToArray();
            SetImageBytes(bytesNew);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var bytes = GetImageBytes();
            var min = bytes.Where(b => b != 0).Min();
            var max = bytes.Max();
            var bytesNew = bytes.Select(b => (byte)(((double)b - min) / (max - min) * 255)).ToArray();
            SetImageBytes(bytesNew);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var bytes = GetImageBytes();
            var hist = new double[256];
            foreach (var b in bytes)
                hist[b]++;

            hist[0] /= bytes.Length;
            for (int i = 1; i < 256; ++i)
            {
                hist[i] /= bytes.Length;
                hist[i] += hist[i - 1];
            }

            var bytesNew = bytes.Select(b => (byte)(hist[b] * 255)).ToArray();
            SetImageBytes(bytesNew);
        }

        List<PointF> points = new List<PointF>();

        private void button8_Click(object sender, EventArgs e)
        {
            if (points.Count == 0)
                throw new Exception("Пустой список точек");
            points = points.OrderBy(p => p.X).ToList();

            Func<byte, byte> convert;
            switch (listBox2.SelectedIndex)
            {
                case 0:
                    ProcessLinear(out convert);
                    break;
                case 1:
                    ProcessSpline(out convert);
                    break;
                default:
                    throw new Exception("Нереализованный вид преобразования");
            }

            var bytes = GetImageBytes();
            var bytesNew = bytes.Select(b => convert(b)).ToArray();
            SetImageBytes(bytesNew);
        }

        private void ProcessLinear(out Func<byte, byte> func)
        {
            var g = pictureBox3.CreateGraphics();
            var pen = new Pen(Color.Red, 2);
            var prev = new PointF(0, 0);
            foreach(var point in points)
            {
                g.DrawLine(pen,
                    prev.X * pictureBox3.Width, (1 - prev.Y) * pictureBox3.Height,
                    point.X * pictureBox3.Width, (1 - point.Y) * pictureBox3.Height);
                prev = point;
            }
            g.DrawLine(pen,
                prev.X * pictureBox3.Width, (1 - prev.Y) * pictureBox3.Height,
                pictureBox3.Width, 0);

            var pointsToCount = new List<PointF>() { new PointF(0, 0) };
            pointsToCount.AddRange(points);
            pointsToCount.Add(new PointF(1, 1));
            func = bt =>
            {
                var arg = bt / 255f;
                var left = pointsToCount.Last(pt => pt.X <= arg);
                var right = pointsToCount.First(pt => pt.X >= arg);
                var val = left.Y + (right.Y - left.Y) * (arg - left.X) / (right.X - left.X);
                return (byte)Math.Min(255, val * 255);
            };
        }

        private void ProcessSpline(out Func<byte, byte> func)
        {
            //var bmp = pictureBox3.Image as Bitmap;
            var g = pictureBox3.CreateGraphics();
            var pen = new Pen(Color.Red, 2);

            var pointsToCount = new List<PointF>() { new PointF(0, 0) };
            pointsToCount.AddRange(points);
            pointsToCount.Add(new PointF(1, 1));
            var dic = pointsToCount.ToDictionary(p => (double)p.X, p => (double)p.Y);
            var interpol = new SplineInterpolator(dic);
            
            foreach (var b in Enumerable.Range(0, 256))
            {
                var arg = b / 255f;
                var pt = new Point((int)(arg * pictureBox3.Width), (int)((1 - interpol.GetValue(arg)) * pictureBox3.Height));
                //bmp.SetPixel(pt.X, pt.Y, Color.Red);
                g.DrawLine(pen, pt.X, pt.Y, pt.X + 1, pt.Y + 1);
            }

            func = bt =>
            {
                var arg = bt / 255f;
                var val = interpol.GetValue(arg);
                return (byte)Math.Min(255, val * 255);
            };
        }

        private void button9_Click(object sender, EventArgs e)
        {
            pictureBox3.Image = null;
            points.Clear();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            var position = (e as MouseEventArgs).Location;
            var x = position.X / (float)pictureBox3.Size.Width;
            var y = 1 - position.Y / (float)pictureBox3.Size.Height;

            points.Add(new PointF(x, y));
            pictureBox3.CreateGraphics().FillEllipse(new SolidBrush(Color.Blue), position.X - 3, position.Y - 1, 7, 7);
        }
    }
}
