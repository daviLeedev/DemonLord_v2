using System.Threading.Tasks;
using DemonLord.Application;

namespace DemonLord.Bootstrap
{
    public interface ISceneFlowService
    {
        Task LoadFrontendAsync();

        Task LoadEntryAsync(EntryDestination destination);
    }
}
