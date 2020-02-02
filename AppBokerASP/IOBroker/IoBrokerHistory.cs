using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.IOBroker
{
    public class IoBrokerHistory
    {
        public IoBrokerHistory(HistoryRecord[] historyRecords, string propertyName)
        {
            HistoryRecords = historyRecords;
            PropertyName = propertyName;
        }
        public IoBrokerHistory()
        {

        }

        public HistoryRecord[] HistoryRecords { get; set; }
        
        public string PropertyName { get; set; }

        public class HistoryRecord
        {
            public float? val { get; set; }
            public long ts { get; set; }
        }

    }
}
