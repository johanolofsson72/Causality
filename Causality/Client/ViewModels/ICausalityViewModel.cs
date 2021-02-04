using System.Threading.Tasks;
using Causality.Client.Shared;
using Microsoft.AspNetCore.Components;

namespace Causality.Client.ViewModels
{
    public interface ICausalityViewModel
    {
        CascadingAppStateProvider StateProvider { get; set; }
        Task AppState_StateChanged(ComponentBase Source, string Property);
        void Dispose();
    }
}