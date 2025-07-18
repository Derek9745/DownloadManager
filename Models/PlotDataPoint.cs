using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace DownloadManagerApp.Models
{
    public class PlotDataPoint
    {
        
        public double SizeInMB { get; set; }
        public DateTime DownloadDate { get; set; }

        public PlotDataPoint() { }

        public PlotDataPoint( DateTime downloadDate, double sizeInMB)
        {
           
            this.DownloadDate = downloadDate;
            this.SizeInMB = SizeInMB;
        }

        public void AddToSize(double downloadSize)
        {
            SizeInMB = SizeInMB + downloadSize;
        }

    }
}
