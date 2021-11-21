using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenshinAcademyBridge.Modules
{
    public class ImageConvertor
    {
        public string apiKey { get; set; }

        public string input { get; set; }

        public string file { get; set; }
        public string filename { get; set; }
        public string outputformat { get; set; }
        public object options { get; set; }
    }
}
