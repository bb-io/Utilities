
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
        public async Task ConvertTestToDate_IssSuccess()
        {
            var action = new Dates(InvocationContext);

            var result =  action.ConvertTextToDate(new TextToDateRequest
            {
                Text = "6/2/2025 16:00:00",
                Timezone = "Asia/Hovd",
            });

            Console.WriteLine(result.Date);
            Assert.IsNotNull(result);
        }
    }
}
