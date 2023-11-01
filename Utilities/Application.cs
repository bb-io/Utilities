using Blackbird.Applications.Sdk.Common;

namespace Apps.Utilities
{
    public class Application : IApplication
    {
        public string Name
        {
            get => "Utilities";
            set { }
        }

        public T GetInstance<T>()
        {
            throw new NotImplementedException();
        }
    }
}