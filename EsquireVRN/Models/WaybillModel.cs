namespace EsquireVRN.Models
{
    public class WaybillModel
    {
        public class Record
        {
            public string delivery_no { get; set; }
            public string base64 { get; set; }
            public string base64_file_type { get; set; }
        }

        public class WayBillResponse
        {
            public string response_message { get; set; }
            public int response_code { get; set; }
            public int record_count { get; set; }
            public List<Record> records { get; set; }
        }
    }
}
