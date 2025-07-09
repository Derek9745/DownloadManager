using CommunityToolkit.Mvvm.ComponentModel;
using DownloadManagerApp.Models;
using DownloadManagerApp.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DownloadManagerApp.ViewModels
{
    public partial class PlotViewModel :ObservableObject
    {


        [ObservableProperty]
        public PlotModel myModel;
        public LineSeries lineSeries;
        private List<PlotDataPoint> dataPointsList = new();
        
        public PlotViewModel(){ 

            MyModel = new PlotModel { Title = "Historic Download Usage" };
            MyModel.TitleColor = OxyPlot.OxyColors.White;
            MyModel.PlotAreaBorderColor = OxyPlot.OxyColors.White;
            MyModel.TextColor = OxyPlot.OxyColors.White;
           
            
            
            DateTime date = DateTime.Now;
           var timeAxes = new DateTimeAxis
            {
                Minimum = DateTimeAxis.ToDouble(DateTime.Now.AddDays(-30)),
                Maximum = DateTimeAxis.ToDouble(DateTime.Now),
                Position = AxisPosition.Bottom,
                Title = "Date", 
                TitleColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                AxislineColor = OxyColors.White
            };

            var sizeAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Downloaded Size (MB)",
                TitleColor = OxyColors.White,
                TicklineColor = OxyColors.White,
                AxislineColor = OxyColors.White
            };

            lineSeries = new LineSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerSize = 2,
                MarkerFill = OxyColors.White,
                Title = "Downloads"
            };

            MyModel.Axes.Add(sizeAxis);
            MyModel.Axes.Add(timeAxes);
            MyModel.Series.Add(lineSeries);
        }

        //initial saved previous downloads
        public void InitializePoints(List<PlotDataPoint> dbPointsList)
        {

            lineSeries.Points.Clear();

            DateTime cutoffDate = DateTime.Now.AddDays(-30);

            dataPointsList = dbPointsList.OrderBy(p => p.DownloadDate).ToList();

            //filter dataPoints List to only contain downloads before the cutoff date
            dbPointsList.RemoveAll(p => p.DownloadDate < cutoffDate);

            for (int i = 0; i < dataPointsList.Count(); i++)
            {
                var x = DateTimeAxis.ToDouble(dataPointsList[i].DownloadDate);
                var y = dataPointsList[i].SizeInMB;
                lineSeries.Points.Add(new DataPoint(x, y));
            }

            MyModel.InvalidatePlot(true);
        }



        public List<PlotDataPoint> UpdatePoints(double downloadSize)
        {
            var x = DateTimeAxis.ToDouble(DateTime.Now);
            var y = downloadSize;

            lineSeries.Points.Add(new DataPoint(x, y));

            
            dataPointsList.Add(new PlotDataPoint
            {
                DownloadDate = DateTime.Now,
                SizeInMB = downloadSize
            });

            MyModel.InvalidatePlot(true);

            return dataPointsList;
        }
    }
}

