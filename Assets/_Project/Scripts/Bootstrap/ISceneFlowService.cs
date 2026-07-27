using System.Threading.Tasks;
using DemonLord.Application;

namespace DemonLord.Bootstrap
{
    public interface ISceneFlowService
    {
        Task LoadFrontendAsync(FrontendEntryMode entryMode);

        Task LoadEntryAsync(EntryDestination destination);
    }
}
