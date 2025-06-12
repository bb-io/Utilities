
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
                Text = "5/30/2025 21:00:00",
                Format = "M/d/yyyy H:mm:ss",
                Timezone = "Asia/Macau"
            };

            var result = action.ConvertTextToDate(request);

            Console.WriteLine(result.Date);
            Assert.IsNotNull(result);
        }
    }
}
