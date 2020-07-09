using System;
using System.Collections.Generic;
using System.Text;

namespace stl_uploader_2
{
    public class JsonFileInfo
    {
        public FileInfo[] files { get; set; }
        public string jobuuid { get; set; }
    }

    public class FileInfo
    {
        public string name { get; set; }
        public int size { get; set; }
        public string type { get; set; }
    }

    public class JsonStatusInfo
    {
        public string action { get; set; }
        public string jobuuid { get; set; }
        public string jobstatus { get; set; }
    }

}
