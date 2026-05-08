using System;
using System.Collections.Generic;
using System.Text;

namespace ApiFunctionTrainReceiveCsv.Models
{
    public class FileCsv
    {

        public string NameFile { get; set; }

        public Stream content { get; set; }
    }
}
