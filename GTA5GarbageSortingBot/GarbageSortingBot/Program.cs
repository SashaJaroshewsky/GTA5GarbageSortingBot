using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsInput;

namespace GarbageSortingBot
{
    internal class Program
    {
        private static readonly InputSimulator _sim = new InputSimulator();

        private static int _trashSearchRegionPositionX = (1920 - 190) / 2;
        private static int _trashSearchRegionPositionY = (1080 - 190) / 2;

        private static readonly Rectangle _trashSearchRegion = new Rectangle(_trashSearchRegionPositionX, _trashSearchRegionPositionY, 190, 190);

        private static readonly List<string> _glassImagePaths = new List<string>
        {
             Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Glass2.png")
        };

        private static readonly List<string> _organicImagePaths = new List<string>
        {
             Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Organic1.png"),
             Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Organic2.png")
        };
        private static readonly List<string> _paperImagePaths = new List<string>
        {
             Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Paper1.png"),
             Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Paper2.png"),
             Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Paper3.png")
        };

        private static readonly List<string> _plasticImagePaths = new List<string>
        {
             Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Plastic1.png"),
             Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Plastic2.png")
        };


        private static readonly List<Mat> _glassTemplates = LoadTemplates(_glassImagePaths);
        private static readonly List<Mat> _organicTemplates = LoadTemplates(_organicImagePaths);
        private static readonly List<Mat> _paperTemplates = LoadTemplates(_paperImagePaths);
        private static readonly List<Mat> _plasticTemplates = LoadTemplates(_plasticImagePaths);

        static async Task Main(string[] args)
        {
            Console.WriteLine("Починається автоматизація сортування...");
            await Task.Delay(5000);
            await Task.WhenAny(
                Task.Run(SortingLoop)
            );
        }

        private static int _miniGameSearchRegionPositionX = 883;
        private static int _miniGameSearchRegionPositionY = 36;

        private static readonly Rectangle _miniGameSearchRegion = new Rectangle(_miniGameSearchRegionPositionX, _miniGameSearchRegionPositionY, 155, 29);

        private static string _miniGameImagePaths = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "MiniGame.png");
        private static readonly Mat _miniGameTemplates = LoadTemplate(_miniGameImagePaths);
        private async static Task SortingLoop()
        {
            while (true)
            {
                using (var screen = CaptureRegion(_miniGameSearchRegion).CvtColor(ColorConversionCodes.BGR2GRAY)
                    //var screen = CaptureFullScreen().CvtColor(ColorConversionCodes.BGR2GRAY)
                    )
                {
                    if (MatchSearch(screen, _miniGameTemplates)
                        //MatchSearchDebug(screen, _miniGameTemplates)
                        )
                    {
                        Console.WriteLine("Починається процес сортування...");
                        await Sorting();
                    }
                    else
                        await Task.Delay(2000);
                }

            }
        }

        private static OpenCvSharp.Point _glassTargetPoint = new OpenCvSharp.Point(202, 830);
        private static OpenCvSharp.Point _organicTargetPoint = new OpenCvSharp.Point(1723, 830);
        private static OpenCvSharp.Point _paperTargetPoint = new OpenCvSharp.Point(1723, 283);
        private static OpenCvSharp.Point _plasticTargetPoint = new OpenCvSharp.Point(202, 283);

        private static async Task Sorting()
        {
            using (var screen = CaptureRegion(_trashSearchRegion).CvtColor(ColorConversionCodes.BGR2GRAY))
            {
                Console.WriteLine("Визнвчення типу сміття...");

                foreach (var glassTemplate in _glassTemplates)
                {
                    if (MatchSearch(screen, glassTemplate))
                    {
                        Console.WriteLine("Це скло! Починаю переміщати...");
                        await MoveTrash(_glassTargetPoint);
                        return;
                    }
                }

                foreach (var organicTemplate in _organicTemplates)
                {
                    if (MatchSearch(screen, organicTemplate))
                    {
                        Console.WriteLine("Це органіка! Починаю переміщати...");
                        await MoveTrash(_organicTargetPoint);
                        return;
                    }
                }

                foreach (var paperTemplate in _paperTemplates)
                {
                    if (MatchSearch(screen, paperTemplate))
                    {
                        Console.WriteLine("Це папір! Починаю переміщати...");
                        await MoveTrash(_paperTargetPoint);
                        return;
                    }
                }

                foreach (var plasticTemplate in _plasticTemplates)
                {
                    if (MatchSearch(screen, plasticTemplate))
                    {
                        Console.WriteLine("Це пластик! Починаю переміщати...");
                        await MoveTrash(_plasticTargetPoint);
                        return;
                    }
                }
            }

            await Task.Delay(10);
        }

        //private async static Task MoveTrash(OpenCvSharp.Point target)
        //{
        //    int screenWidth = 1920;
        //    int screenHeight = 1080;

        //    double normalizedX = ((screenWidth / 2) * 65535) / screenWidth;
        //    double normalizedY = ((screenHeight / 2) * 65535) / screenHeight;

        //    double normalizedTargetX = (target.X * 65535) / screenWidth;
        //    double normalizedTargetY = (target.Y * 65535) / screenHeight;

        //    _sim.Mouse.MoveMouseTo(normalizedX, normalizedY);
        //    _sim.Mouse.LeftButtonDown();
        //    await Task.Delay(25);
        //    _sim.Mouse.MoveMouseTo(normalizedTargetX, normalizedTargetY);
        //    _sim.Mouse.LeftButtonUp();
        //    await Task.Delay(50);

        //}

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);



        private async static Task SmoothMove(int startX, int startY, int targetX, int targetY, int steps, int delay)
        {
            double stepX = (targetX - startX) / (double)steps;
            double stepY = (targetY - startY) / (double)steps;

            for (int i = 0; i <= steps; i++)
            {
                int currentX = (int)(startX + stepX * i);
                int currentY = (int)(startY + stepY * i);
                SetCursorPos(currentX, currentY);
                await Task.Delay(delay);
            }
        }

        private async static Task MoveTrash(OpenCvSharp.Point target, int steps = 10, int speed = 1)
        {
            int screenWidth = 1920;
            int screenHeight = 1080;

            // Поточні координати миші (центр екрану)
            int startX = screenWidth / 2;
            int startY = screenHeight / 2;

            // Кінцеві координати цілі
            int targetX = target.X;
            int targetY = target.Y;

            // Плавний рух до центру екрану
            //await Task.Delay(20);
            await SmoothMove(startX, startY, startX, startY, steps, speed);

            _sim.Mouse.LeftButtonDown();
            await Task.Delay(10); // Додаткова затримка після натискання кнопки

            // Плавний рух до цілі
            await SmoothMove(startX, startY, targetX, targetY, steps, speed);

            _sim.Mouse.LeftButtonUp();
            await Task.Delay(1); // Додаткова затримка після переміщення
        }

        //[DllImport("user32.dll")]
        //static extern bool SetCursorPos(int X, int Y);

        //[DllImport("user32.dll")]
        //static extern bool GetCursorPos(out POINT lpPoint);

        //// Структура для збереження координат курсора
        //[StructLayout(LayoutKind.Sequential)]
        //public struct POINT
        //{
        //    public int X;
        //    public int Y;
        //}

        //// Метод для плавного переміщення миші
        //private async static Task SmoothMove(int startX, int startY, int targetX, int targetY, int steps, int delay)
        //{
        //    double stepX = (targetX - startX) / (double)steps;
        //    double stepY = (targetY - startY) / (double)steps;

        //    for (int i = 0; i <= steps; i++)
        //    {
        //        int currentX = (int)(startX + stepX * i);
        //        int currentY = (int)(startY + stepY * i);
        //        SetCursorPos(currentX, currentY);
        //        await Task.Delay(delay);
        //    }
        //}

        //private async static Task MoveTrash(OpenCvSharp.Point target, int steps = 20, int speed = 1)
        //{
        //    int screenWidth = 1920;
        //    int screenHeight = 1080;

        //    // Поточні координати миші
        //    GetCursorPos(out POINT currentPos);
        //    int startX = currentPos.X;
        //    int startY = currentPos.Y;

        //    // Центр екрану
        //    int centerX = screenWidth / 2;
        //    int centerY = screenHeight / 2;

        //    // Кінцеві координати цілі
        //    int targetX = target.X;
        //    int targetY = target.Y;

        //    // Плавний рух до центру екрану
        //    await SmoothMove(startX, startY, centerX, centerY, steps, speed);

        //    _sim.Mouse.LeftButtonDown();
        //    await Task.Delay(5); // Додаткова затримка після натискання кнопки

        //    // Плавний рух до цілі
        //    await SmoothMove(centerX, centerY, targetX, targetY, steps, speed);

        //    _sim.Mouse.LeftButtonUp();
        //    await Task.Delay(5); // Додаткова затримка після переміщення
        //}

        private static bool MatchSearch(Mat screen, Mat template)
        {
            Console.WriteLine("Порівнюю жображення...");
            using (var result = new Mat())
            {
                Cv2.MatchTemplate(screen, template, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point position);

                if (maxVal >= 0.7)
                {
                    Console.WriteLine("Є співпадіння");
                    return true;
                }
                Console.WriteLine("Немає співпадіння");
                return false;
            }
        }

        private static bool MatchSearchDebug(Mat screen, Mat template)
        {
            using (var result = new Mat())
            {
                Cv2.MatchTemplate(screen, template, result, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out OpenCvSharp.Point position);

                if (maxVal >= 0.8)
                {
                    Console.WriteLine($"{nameof(template)}: X = {position.X}, Y = {position.Y}");
                    return true;
                }
                return false;
            }
        }

        static Mat CaptureRegion(Rectangle region)
        {
            using (var bmp = new Bitmap(region.Width, region.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(region.X, region.Y, 0, 0, bmp.Size);
                }
                return BitmapConverter.ToMat(bmp);
            }
        }

        static Mat CaptureFullScreen()
        {
            using (var bmp = new Bitmap(1920, 1080))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                }
                return BitmapConverter.ToMat(bmp);
            }
        }

        static List<Mat> LoadTemplates(List<string> paths)
        {
            var templates = new List<Mat>();
            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    templates.Add(Cv2.ImRead(path, ImreadModes.Grayscale));
                }
                else
                {
                    Console.WriteLine($"Файл {path} не знайдено.");
                }
            }
            return templates;
        }

        static Mat LoadTemplate(string path)
        {
            var template = new Mat();

            if (File.Exists(path))
            {
                template = Cv2.ImRead(path, ImreadModes.Grayscale);
            }
            else
            {
                Console.WriteLine($"Файл {path} не знайдено.");
            }
            return template;
        }
    }
}
