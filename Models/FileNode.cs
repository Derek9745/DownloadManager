using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DownloadManagerApp.Models
{
    public class FileNode
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public bool IsFolder { get; set;}
        public List<FileNode> Children { get; set; } = new (); 
        public string Icon
        {
            get
            {
                if (IsFolder)
                {
                    return "Folder";
                }
                else
                {
                    return "FileDocument";
                }
            }
        }

        public FileNode(string name, string id, bool isFolder)
        {
            Name = name;
            Id = id;
            IsFolder = isFolder;
        }
    }
}
