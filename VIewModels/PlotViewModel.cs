using CommunityToolkit.Mvvm.ComponentModel;
using DownloadManagerApp.Models;
using DownloadManagerApp.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
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
           
            
            
            DateTime date = DateTime.Today;
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

        //initialize previous downloads stored in the databasePointsList
        public void InitializePoints(List<PlotDataPoint> dbPointsList)
        {

            lineSeries.Points.Clear();
            DateTime cutoffDate = DateTime.Today.AddDays(-30);

            // Filter first, then assign
            dataPointsList = dbPointsList
                .Where(p => p.DownloadDate >= cutoffDate)
                .OrderBy(p => p.DownloadDate)
                .ToList();


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
            //check to see if a point already exists with the same date in the datapointslist
            bool fileDownloadedTodayAlready = dataPointsList.Exists(p => p.DownloadDate == DateTime.Today);

            if(fileDownloadedTodayAlready == true)
            {
                lineSeries.Points.Clear();

                
                var existingPoint = dataPointsList.FirstOrDefault(p => p.DownloadDate == DateTime.Today);

                if (existingPoint != null)
                {
                    existingPoint.AddToSize(downloadSize);
                }
               

                // reload all datapoints from the dataPointList into the lineSeries
                for (int i = 0; i < dataPointsList.Count(); i++)
                {
                    var x = DateTimeAxis.ToDouble(dataPointsList[i].DownloadDate);
                    var y = dataPointsList[i].SizeInMB;

                    lineSeries.Points.Add(new DataPoint(x, y));
                }

            }
            else
            {
                var newPoint = new PlotDataPoint(DateTime.Today, downloadSize);
                dataPointsList.Add(newPoint);
                lineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(DateTime.Today), downloadSize));
            }

            

            MyModel.InvalidatePlot(true);
            return dataPointsList;
        }
    }
}

