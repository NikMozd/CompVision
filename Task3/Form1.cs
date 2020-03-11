using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Task3
{
    public partial class Form1 : Form
    {
        public class Coords
        {
            public int row;
            public int col;

            public override bool Equals(object obj)
            {
                return obj is Coords coords && this.col == coords.col && this.row == coords.row;
            }
        }

        private string path = "..\\..\\Шестеренки.png";
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
            return res.Where((b, i) => i % 3 == 0).Select(b => b != 0 ? (byte)255 : (byte)0).ToArray();
        }

        private void SetImageBytes(byte[] bytes)
        {
            bytes = bytes.SelectMany(b => new[] { b, b, b }).ToArray();
            var bmp = (Bitmap)pictureBox2.Image;
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, bmpData.Scan0, bytes.Length);
            bmp.UnlockBits(bmpData);
        }

        private byte[][] To2Dimensions(byte[] bytes, int w, int h)
        {
            var result = new byte[h][];
            for (int i = 0; i < h; ++i)
                result[i] = bytes.Skip(w * i).Take(w).ToArray();
            return result;
        }

        private byte[] To1Dimension(byte[][] bytes)
        {
            var result = new List<byte>(bytes.Length);
            foreach (var subarr in bytes)
                result.AddRange(subarr);
            return result.ToArray();
        }

        private byte[][] Dilation(byte[][] image, byte[][] elem)
        {
            var w = image.GetLength(0);
            var h = image[0].GetLength(0);
            var mw = elem.GetLength(0);
            var mh = elem[0].GetLength(0);

            /*
            var elem1D = To1Dimension(elem);
            var positions = elem1D
                .Select((b, i) => new { Bit = b, Row = i / mw - mw / 2, Col = i % mw - mh / 2 })
                .Where(s => s.Bit != 0)
                .Select(s => new Coords { row = s.Row, col = s.Col });

            var image1D = To1Dimension(image);
            var imageWithPos = image1D
                .Select((b, i) => new { Bit = b, Coords = new Coords { row = i / w, col = i % w } });

            Predicate<Coords> innerPoint = c => c.row >= mw / 2 && c.row <= w - mw / 2
                    && c.col >= mh / 2 && c.col <= h - mh / 2;

            var result = imageWithPos
                .Select(temp => !innerPoint(temp.Coords)
                    ? (byte)0
                    : imageWithPos.Where(pos =>
                            positions.Select(coord => new Coords { row = coord.row + temp.Coords.row, col = coord.col + temp.Coords.col })
                            .ToHashSet().Contains(pos.Coords)
                        ).Max(pos => pos.Bit))
                .ToArray();
            return To2Dimensions(result, w, h);
            */
            
            byte[][] result = new byte[w][];
            for (int i = 0; i < w; ++i)
                result[i] = new byte[h];

            for (var x = mw / 2; x < w - mw / 2; ++x)
                for (var y = mh / 2; y < h - mh / 2; ++y)
                {
                    byte max = 0;
                    for (int i = -mw / 2; i <= mw / 2; ++i)
                        for (int j = -mh / 2; j <= mh / 2; ++j)
                            if (elem[i + mw / 2][j + mh / 2] == 255 && image[x + i][y + j] > max)
                                max = image[x + i][y + j];
                    result[x][y] = max;
                }

            return result;
        }

        private byte[][] Erosion(byte[][] image, byte[][] elem)
        {
            var w = image.GetLength(0);
            var h = image[0].GetLength(0);
            var mw = elem.GetLength(0);
            var mh = elem[0].GetLength(0);

            byte[][] result = new byte[w][];
            for (int i = 0; i < w; ++i)
                result[i] = new byte[h];

            for (var x = mw / 2; x < w - mw / 2; ++x)
                for (var y = mh / 2; y < h - mh / 2; ++y)
                {
                    byte min = 255;
                    for (int i = -mw / 2; i <= mw / 2; ++i)
                        for (int j = -mh / 2; j <= mh / 2; ++j)
                            if (elem[i + mw / 2][j + mh / 2] == 255 && image[x + i][y + j] < min)
                                min = image[x + i][y + j];
                    result[x][y] = min;
                }

            return result;
        }

        private byte[][] Closing(byte[][] image, byte[][] elem) => Erosion(Dilation(image, elem), elem);

        private byte[][] Opening(byte[][] image, byte[][] elem) => Dilation(Erosion(image, elem), elem);

        private byte[][] HoleDisk(int length)
        {
            var bmp = new Bitmap(length, length);
            var g = Graphics.FromImage(bmp);
            g.DrawEllipse(new Pen(Color.White), 0, 0, length, length);

            byte[][] result = new byte[length][];
            for (int i = 0; i < length; ++i)
            {
                result[i] = new byte[length];
                for (int j = 0; j < length; ++j)
                    result[i][j] = bmp.GetPixel(i, j).A != 0 ? (byte)255 : (byte)0;
            }

            g.Dispose();
            return result;
        }

        private byte[][] FillDisk(int length)
        {
            byte[][] result = new byte[length][];
            for (int i = 0; i < length; ++i)
                result[i] = new byte[length];
            for (int i = 0; i < length; ++i)
                for (int j = 0; j < length; ++j)
                    result[i][j] = 255;
            return result;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Image.FromFile(path);
            pictureBox1.Refresh();

            var bytes = To2Dimensions(GetImageBytes(), pictureBox1.Image.Width, pictureBox1.Image.Height);
            var elem = HoleDisk(25);

            /*
            var s1 = new StringBuilder();
            foreach (var arr in elem)
                s1.Append(string.Join("", arr) + '\n');
            var r1 = s1.ToString();

            var s2 = new StringBuilder();
            for (int i = 83; i < 178; ++i)
            {
                for (int j = 433; j < 528; ++j)
                    s2.Append(bytes[i][j]);
                s2.Append('\n');
            }
            var r2 = s2.ToString();
            */

            var bytesNew = Dilation(bytes, elem);
            SetImageBytes(To1Dimension(bytesNew));
        }
    }
}
