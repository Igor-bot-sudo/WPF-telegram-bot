using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfTelegramBot
{
    class Bitcoin
    {
        public double currency_rate;

        private MainWindow w;
        private double minY;
        private double maxY;
        private bool non_null_rate = false;
        private readonly ObservableCollection<double> chartItems = new ObservableCollection<double>();
        private System.Windows.Threading.DispatcherTimer timer;


        public Bitcoin(MainWindow W)
        {
            this.w = W;
        }

        public async void GetRate()
        {
            string answer = "";
            // Строка запроса для сайта blockchain.info
            string requestString = "https://blockchain.info/ru/ticker";

            WebRequest request = WebRequest.Create(requestString);
            request.Method = "GET";
            WebResponse response = await request.GetResponseAsync();

            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    answer = reader.ReadToEnd();
                }
            }

            // Парсинг данных без обработки JSON-файла
            Regex regex = new Regex("RUB.*last.*RUB", RegexOptions.IgnoreCase);
            if (!regex.IsMatch(answer))
            {
                return;
            }

            MatchCollection mc = regex.Matches(answer);
            if (mc.Count == 0)
            {
                return;
            }

            regex = new Regex(@"[\d]*[\.][\d][\d]", RegexOptions.IgnoreCase);

            foreach (Match item in mc)
            {
                MatchCollection mc2 = regex.Matches(item.Value);
                if (mc2.Count == 0)
                {
                    return;
                }
                currency_rate = Convert.ToDouble(mc2[0].Value.Split('.')[0]);
                return;
            }
        }

        public void StartBitcoinChart()
        {
            // Создаем таймер
            timer = new System.Windows.Threading.DispatcherTimer();

            // Регистрируем обработчик событий таймера
            timer.Tick += new EventHandler(timerTick);

            // Задаем интервал срабатывания таймера
            timer.Interval = new TimeSpan(0, 0, 1);

            // Запускаем таймер
            timer.Start();
        }

        private void timerTick(object sender, EventArgs e)
        {
            // Текущий курс
            GetRate();

            if (!non_null_rate && currency_rate != 0)
            {
                non_null_rate = true;
                return;
            }

            if (non_null_rate)
            {
                // Если добрались до края, сдвигаем все влево
                if (chartItems.Count == (int)w.chartCanvas.ActualWidth)
                {
                    chartItems.RemoveAt(0);
                }
                chartItems.Add(currency_rate);
            }

            minY = FindMin() - 50;
            maxY = FindMax() + 50;

            Сhart();
        }



        private double FindMin()
        {
            double result;

            if (chartItems.Count == 0)
            {
                result = currency_rate;
            }
            else
            {
                result = chartItems[0];
                for (int i = 0; i < chartItems.Count; i++)
                {
                    if (chartItems[i] < result)
                    {
                        result = chartItems[i];
                    }
                }
            }
            return result;
        }

        private double FindMax()
        {
            double result;

            if (chartItems.Count == 0)
            {
                result = currency_rate;
            }
            else
            {
                result = chartItems[0];
                for (int i = 0; i < chartItems.Count; i++)
                {
                    if (chartItems[i] > result)
                    {
                        result = chartItems[i];
                    }
                }
            }
            return result;
        }

        // Функция преобразования координат из данных в пиксели
        private double MapToPixel(double v, double minV, double maxV, double pixelMax)
        {
            double k, d, result;
            double offset = 20;

            k = (pixelMax - 2 * offset) / (maxV - minV);
            d = offset - k * minV;
            result = k * v + d;

            return result;
        }


        private void Сhart()
        {
            Canvas.SetTop(w.chartCanvas, 0);
            Canvas.SetRight(w.chartCanvas, 0);
            Canvas.SetTop(w.textCanvas, 5);

            // Очистка холстов
            List<UIElement> uieList = new List<UIElement>();
            foreach (UIElement item in w.textCanvas.Children)
            {
                if (item is TextBlock)
                {
                    uieList.Add(item);
                }
            }

            for (int i = 0; i < uieList.Count; i++)
            {
                w.textCanvas.Children.Remove(uieList[i]);
            }

            w.chartCanvas.Children.Clear();


            var pixelWidth = w.chartCanvas.ActualWidth;
            var pixelHeight = w.chartCanvas.ActualHeight;

            // Диаграмма
            Polyline pl = new Polyline();
            pl.Stroke = new SolidColorBrush(Colors.Blue);
            pl.Fill = new SolidColorBrush(Color.FromRgb(235, 250, 250));
            pl.StrokeThickness = 1;
            pl.Width = pixelWidth;
            pl.Height = pixelHeight;

            PointCollection points = new PointCollection(chartItems.Count + 2);
            points.Add(new Point(0, pixelHeight));
            double pixelY = 0;
            for (int pixelX = 0; pixelX < chartItems.Count; pixelX++)
            {
                var y = chartItems[pixelX];
                pixelY = pixelHeight - MapToPixel(y, minY, maxY, pixelHeight);
                points.Add(new Point(pixelX, pixelY));
            }
            points.Add(new Point(chartItems.Count - 1, pixelHeight));
            pl.Points = points;
            w.chartCanvas.Children.Add(pl);

            // Оси
            Line Line = new Line();
            Line.Stroke = Brushes.Black;
            Line.StrokeThickness = 1;
            Line.X1 = 0;
            Line.Y1 = 0;
            Line.X2 = 0;
            Line.Y2 = pixelHeight;
            w.chartCanvas.Children.Add(Line);

            Line = new Line();
            Line.Stroke = Brushes.Black;
            Line.StrokeThickness = 1;
            Line.X1 = 0;
            Line.Y1 = pixelHeight;
            Line.X2 = pixelWidth;
            Line.Y2 = pixelHeight;
            w.chartCanvas.Children.Add(Line);

            // Текущий уровень
            Line gridLine = new Line();
            gridLine.X1 = 0;
            gridLine.Y1 = pixelY;
            gridLine.X2 = pixelWidth;
            gridLine.Y2 = pixelY;
            gridLine.Stroke = Brushes.Gray;
            gridLine.StrokeThickness = 1;
            gridLine.StrokeDashArray = new DoubleCollection(new double[2] { 4, 3 });
            w.chartCanvas.Children.Add(gridLine);

            TextBlock tb = new TextBlock();
            if (chartItems.Count != 0)
            {
                tb.Text = chartItems[chartItems.Count - 1].ToString();
            }
            tb.FontSize = 13;
            tb.FontWeight = FontWeights.SemiBold;
            tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            Size size = tb.DesiredSize;
            w.textCanvas.Children.Add(tb);
            Canvas.SetLeft(tb, 10);
            Canvas.SetTop(tb, pixelY - size.Height / 2);

            // Максимальный уровень
            double pixelYmax = pixelHeight - MapToPixel(maxY, minY, maxY, pixelHeight);
            gridLine = new Line();
            gridLine.X1 = 0;
            gridLine.Y1 = pixelYmax;
            gridLine.X2 = pixelWidth;
            gridLine.Y2 = pixelYmax;
            gridLine.Stroke = Brushes.Green;
            gridLine.StrokeThickness = 1;
            gridLine.StrokeDashArray = new DoubleCollection(new double[2] { 4, 3 });
            w.chartCanvas.Children.Add(gridLine);

            if (Math.Abs(pixelYmax - pixelY) > 15)
            {
                tb = new TextBlock();
                if (chartItems.Count != 0 && chartItems[chartItems.Count - 1] != (maxY - 50))
                {
                    tb.Text = (maxY - 50).ToString();
                }
                tb.FontSize = 13;
                tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                size = tb.DesiredSize;
                w.textCanvas.Children.Add(tb);
                Canvas.SetLeft(tb, 10);
                Canvas.SetTop(tb, pixelYmax - size.Height / 2);
            }

            // Минимальный уровень
            double pixelYmin = pixelHeight - MapToPixel(minY, minY, maxY, pixelHeight);
            gridLine = new Line();
            gridLine.X1 = 0;
            gridLine.Y1 = pixelYmin;
            gridLine.X2 = pixelWidth;
            gridLine.Y2 = pixelYmin;
            gridLine.Stroke = Brushes.Red;
            gridLine.StrokeThickness = 1;
            gridLine.StrokeDashArray = new DoubleCollection(new double[2] { 4, 3 });
            w.chartCanvas.Children.Add(gridLine);

            if (Math.Abs(pixelYmin - pixelY) > 15)
            {
                tb = new TextBlock();
                if (chartItems.Count != 0 && chartItems[chartItems.Count - 1] != (minY + 50))
                {
                    tb.Text = (minY + 50).ToString();
                }
                tb.FontSize = 13;
                tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                size = tb.DesiredSize;
                w.textCanvas.Children.Add(tb);
                Canvas.SetLeft(tb, 10);
                Canvas.SetTop(tb, pixelYmin - size.Height / 2);
            }

            // Временные метки
            DateTime ct = DateTime.Now;
            int k = 0;
            DateTime varTime = ct.AddSeconds(-1 * (chartItems.Count - 1));
            do
            {
                if (varTime.Second == 0)
                {
                    Line = new Line();
                    Line.Stroke = Brushes.Black;
                    Line.StrokeThickness = 1;
                    Line.X1 = k;
                    Line.Y1 = pixelHeight;
                    Line.X2 = k;
                    Line.Y2 = pixelHeight - 7;
                    w.chartCanvas.Children.Add(Line);

                    tb = new TextBlock();
                    tb.Text = varTime.ToString("HH:mm:ss");
                    tb.FontSize = 10;
                    tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                    size = tb.DesiredSize;
                    w.textCanvas.Children.Add(tb);
                    Canvas.SetLeft(tb, 46 + k);
                    Canvas.SetTop(tb, 285);
                }
                k += 1;
                varTime = varTime.AddSeconds(1);
            } while (varTime < ct);
        }
    }
}
