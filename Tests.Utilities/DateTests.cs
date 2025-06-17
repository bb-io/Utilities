
using Apps.Utilities.Actions;
using Apps.Utilities.Models.Dates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests.Utilities.Base;

namespace Tests.Utilities
{
    [TestClass]
    public class DateTests : TestBase
    {
        [TestMethod]
        public async Task ConvertTextToDate_IssSuccess()
        {
            var action = new Dates(InvocationContext);

            var request = new TextToDateRequest
            {
                Text = "6/30/2025 21:00:00",
                //Format = "M/d/yyyy H:mm:ss",
                Timezone = "Asia/Macau"
            };

            var result = action.ConvertTextToDate(request);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(json);
            Assert.IsNotNull(result);
        }
    }
}
