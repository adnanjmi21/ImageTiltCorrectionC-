using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV.Util;
using System.IO;
using System.Drawing.Imaging;

namespace ConsoleApp5
{
	class Program
	{
		static void Main(string[] args)
		{
			string base64String="";
			try
			{
				//read image from disk as emgu cv image
				Image<Bgr, Byte> My_Image = new Image<Bgr, byte>("C:\\Users\\Adnan\\Pictures\\chola18tilt.jpg");
			
				//convert image to array of bytes if jpeg format with quality at 80				
				var bytes = My_Image.ToJpegData(80);

				//convert bytes base 64 string
				base64String = Convert.ToBase64String(bytes);
			}
			catch (NullReferenceException)
			{
			}
			catch (FileLoadException)
			{ 
			}
			
			// call method   to find tilt and return corrected image as base64
			String str = correctTiltImage64(base64String);

			//convert coorrected base64string to image and display output
			var img2 = StringToImage(str);
			Bitmap bmp2 = new Bitmap(img2);

			Image<Bgr, byte> outputImage2 = new Image<Bgr, byte>(bmp2);
			CvInvoke.Imshow("rotated", outputImage2);
			CvInvoke.WaitKey(0);
			
		}
		static double slope(Point p1, Point p2)
		{
			double slope = 0;
			int x = p2.X - p1.X;
			int y = p2.Y - p1.Y;
			if (x == 0)
			{
				return 0;
			}

			slope = (double)y / (double)x;
			return slope;
		
		}

		//method returns image from base64 string
		public static Image StringToImage(String base64String)
		{
			if (String.IsNullOrWhiteSpace(base64String))
				return null;

			var bytes = Convert.FromBase64String(base64String);
			var stream = new MemoryStream(bytes);
			return Image.FromStream(stream);
		}

		public static String correctTiltImage64(String base64String)
		{
			
			if (String.IsNullOrWhiteSpace(base64String))
				return null;
			//convert base64 to Systems. Drwawing image
			var img = StringToImage(base64String);

			// convert image to bitmap
			Bitmap bmp = new Bitmap(img);

			//convert image to emgu cv image
			Image<Bgr, Byte> My_Image = new Image<Bgr, byte>(bmp);
			
			// convert image to gray scale
			Image<Gray, Byte> result = new Image<Gray, byte>(My_Image.ToBitmap());
			UMat u = result.ToUMat();

			//apply canny edge detection 
			CvInvoke.Canny(result, result, 150, 50,3);
			double angle;
			
			// detect lines by applying houghline transform
			LineSegment2D[] lines;
			using (var vector = new VectorOfPointF())
			{
				CvInvoke.HoughLines(result, vector,
					1,
					Math.PI / 180,
					350);

				var linesList = new List<LineSegment2D>();
				Point pt1 = new Point(); ;
				Point pt2 = new Point(); ;

				float theta = 0;

				for (var i = 0; i < vector.Size; i++)
				{
					var rho = vector[i].X;
					theta = vector[i].Y;
					var a = Math.Cos(theta);
					var b = Math.Sin(theta);
					var x0 = a * rho;
					var y0 = b * rho;
					pt1.X = (int)Math.Round(x0 + 1000 * (-b));
					pt1.Y = (int)Math.Round(y0 + 1000 * (a));
					pt2.X = (int)Math.Round(x0 - 1000 * (-b));
					pt2.Y = (int)Math.Round(y0 - 1000 * (a));

					linesList.Add(new LineSegment2D(pt1, pt2));
				}
				if (theta == 0)
				{
					int angle90 = 90;
					float height = My_Image.Height;
					float width = My_Image.Width;
					float cx = width / 2f;
					float cy = height / 2f;

					RotationMatrix2D rotationMatrix1 = new RotationMatrix2D(new PointF(My_Image.Width / 2f, My_Image.Height / 2f), angle90, 1);
					Size imgsize1 = My_Image.Size;
					// rotate image by 270 clockwise
					img.RotateFlip(RotateFlipType.Rotate270FlipNone);
					My_Image = new Image<Bgr, byte>(new Bitmap(img));
					
				}
				else
				{
					//draw line to calculate tilt
					//My_Image.Draw(new LineSegment2D(pt1, pt2), new Bgr(Color.Red), 2);
					//refrence horizontal line
					//My_Image.Draw(new LineSegment2D(new Point(pt1.X, pt1.Y), new Point(pt2.X, pt1.Y)), new Bgr(Color.Green), 2);
					
					//find slope and calculate angle
					angle = slope(pt1, pt2)- slope(new Point(pt1.X, pt1.Y), new Point(pt2.X, pt1.Y));
					angle = Math.Atan(angle);
					angle = (180 / Math.PI) * angle;
					//rotate counter clock wise
					RotationMatrix2D rotationMatrix = new RotationMatrix2D(new PointF(My_Image.Width / 2f, My_Image.Height / 2f), angle, 1);
					Size imgsize = My_Image.Size;
					CvInvoke.WarpAffine(My_Image, My_Image, rotationMatrix, imgsize, Inter.Cubic, Warp.Default, BorderType.Replicate, default(MCvScalar));

				}
				
				lines = linesList.ToArray();
			}

			//convert corrected image to base64 string
			var bytes = My_Image.ToJpegData(80);

			base64String = Convert.ToBase64String(bytes);

			

			return base64String;
		}
	}
}
