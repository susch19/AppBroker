namespace AppBrokerASP.IOBroker
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
            HistoryRecords = Array.Empty<HistoryRecord>();
            PropertyName = "";
        }

        public HistoryRecord[] HistoryRecords { get; set; }
        
        public string PropertyName { get; set; }

        public IoBrokerHistory(string propertyName) : this()
        {
            PropertyName = propertyName;
        }

        public class HistoryRecord
        {
            public float? val { get; set; }
            public long ts { get; set; }
        }

    }
}
