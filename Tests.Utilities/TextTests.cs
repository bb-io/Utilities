using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.Utilities.Actions;
using Apps.Utilities.Models.Files;
using Blackbird.Applications.Sdk.Common.Files;
using Blackbird.Applications.Sdk.Common.Invocation;
using Tests.Utilities.Base;

namespace Tests.Utilities
{
    [TestClass]
    public class TextTests : TestBase
    {
        private Texts _textActions;

        [TestInitialize]
        public void Init()
        {
            var outputDirectory = Path.Combine(GetTestFolderPath(), "Output");
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
            Directory.CreateDirectory(outputDirectory);

            _textActions = new Texts(InvocationContext);
        }

        [TestMethod]
        public async Task TrimText_TrimSizeLongerThanText_ReturnsEmptyString()
        {

            var text = "not a very long text";

            var result = _textActions.TrimText(new Apps.Utilities.Models.Texts.TextDto() { Text = text},new Apps.Utilities.Models.Texts.TrimTextInput() { CharactersFromEnd = 60000});


            Assert.Equals(result.Length, 0);
        }
    }
}
