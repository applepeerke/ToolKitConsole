// Amendment PHE 2016-05-21: This comment only to check it in in TFS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GeneralUtilities
{
    public class JournalModel
    {
        public int JournalId { get; }
        public DateTime TimeStamp { get; }
        public string UserName { get; set; }
        public JournalSource Source { get; set; }
        public string Entity { get; set; }
        public int Key { get; set; } 
        public JournalImage Image { get; set; }
        public JournalOperation Operation { get; set; }
        public JournalResult Result { get; set; }
        public string Data { get; set; }
        public string UserParameter { get; set; }
        public string SpareKey1 { get; set; }
        public int SpareKey2 { get; set; }
    }
}
