using System.Threading.Tasks;

namespace Microwave.Application
{
    public interface IEventDelegateHandler
    {
        Task Update();
    }
}