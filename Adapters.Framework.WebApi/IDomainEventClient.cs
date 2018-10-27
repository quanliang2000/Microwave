using System.Net.Http;
using Application.Framework;

namespace Adapters.Framework.WebApi
{
    public class DomainEventClient<T> : HttpClient
    {
        public DomainEventClient(EventLocationConfig config)
        {
            BaseAddress = config.GetLocationFor(typeof(T).Name);
        }
    }
}