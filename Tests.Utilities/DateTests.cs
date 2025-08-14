
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
                Text = "30/04/2025",
                Format = "dd/MM/yyyy ",
                //Timezone = "Asia/Macau"
            };

            var result = action.ConvertTextToDate(request);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(json);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GenerateDate_IssSuccess()
        {
            var action = new Dates(InvocationContext);

            var request = new GenerateDateRequest
            {
                BusinessDays = -4
            };

            var result = action.GenerateDate(request);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(json);
            Assert.IsNotNull(result);
        }
    }
}
