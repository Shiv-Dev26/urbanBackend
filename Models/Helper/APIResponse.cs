using System.Net;

namespace urbanBackend.Models.Helper
{
    public class APIResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Messages { get; set; }
        public object Data { get; set; }
    }
    public class DistanceMatrixAPIResponse
    {
        public string distance { get; set; }
        public string duration { get; set; }
    }
}
