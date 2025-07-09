using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadManagerApp.Config
{
        public static class AppConfiguration
        {
            public static IConfiguration Configuration { get; }

            static AppConfiguration()
            {
                Configuration = new ConfigurationBuilder()
                    .AddUserSecrets<MainViewModel>()
                    .Build();
            }
        }

}
