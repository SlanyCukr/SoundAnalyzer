using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SoundAnalyzer
{
    static class LedAPI
    {
        private static readonly HttpClient Client = new HttpClient();

        // TODO - IP of the server should be set in some sort of settings
        public async static Task<HttpResponseMessage> RealTime(string condensedBuffer)
        {
            var encodedValues = new FormUrlEncodedContent(new Dictionary<string, string> { { "values", condensedBuffer } });
            return await Client.PostAsync("http://192.168.0.114:5000/real_time", encodedValues);
        }
    }
}
