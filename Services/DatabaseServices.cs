using DownloadManagerApp.Models;
using OxyPlot;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace DownloadManagerApp.Services
{
    public class DatabaseServices
    {
        private SQLiteAsyncConnection connection;

        public DatabaseServices(string path)
        {
            connection = new SQLiteAsyncConnection(path);    
        }

        public async Task InitializeDatabaseAsync()
        {
            await connection.CreateTableAsync<PlotDataPoint>();
        }

        public async Task<List<PlotDataPoint>> GetDataPoints()
        {
            //return all datapoints in the database as a list to the plotviewmodel
            return await connection.Table<PlotDataPoint>().ToListAsync();
        }

        public async Task InsertDataPointAsync(PlotDataPoint point)
        {
            await connection.InsertAsync(point);
        }

        public async Task CleanTable()
        {
            DateTime cutoffDate = DateTime.Now.AddDays(-30);

            //need to add parameters for deleting by cutoff date
            await connection.Table<PlotDataPoint>().DeleteAsync(p => p.DownloadDate < cutoffDate);
            
        }

    }
}
