using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Causality.Client.Services;
using Causality.Client.Shared;
using Causality.Shared.Models;
using Ipify;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

namespace Causality.Client.ViewModels
{
    public class BookingCustomerQueuesViewModel : ComponentBase, ICausalityViewModel, IDisposable
    {
        #region StateProvider
        [CascadingParameter]
        public CascadingAppStateProvider StateProvider { get; set; }

        protected override void OnInitialized() => StateProvider.AppState.StateChanged += async (Source, Property) => await AppState_StateChanged(Source, Property);

        public void Dispose() => StateProvider.AppState.StateChanged -= async (Source, Property) => await AppState_StateChanged(Source, Property);

        public async Task AppState_StateChanged(ComponentBase Source, string Property)
        {
            if (Source != this)
            {
                // Inspect string Property to determine if action needs to be taken.
                // maybe we want to do something before we update the state and rerender?

                //if (Property.Equals("UseIndexedDB"))
                //{

                //}
                //else if (Property.Equals("TimeToLiveInSeconds"))
                //{

                //}
                //else if (Property.Equals("OfflineMode"))
                //{

                //}
                await InvokeAsync(StateHasChanged);
            }

            // Här ska det sparas ett object till localStorage
            await StateProvider.SaveChangesAsync();
        }
        #endregion

        [Parameter] public int BookingCustomerId { get; set; }
        [Parameter] public EventCallback OnAdded { get; set; }
        [Parameter] public EventCallback<Dictionary<string, string>> NotifyParent { get; set; }

        [Inject] Services.UserService UserManager { get; set; }
        [Inject] Services.MetaService MetaManager { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "Queues";
        protected List<BookingCustomer> list = new();
        protected BookingCustomer BookingCustomer = new();

        protected override async Task OnInitializedAsync()
        {
            await Task.Delay(0);

            //// Load data
            //await GetAll();

            //// Invoke StateHasChange
            //await InvokeAsync(StateHasChanged);
        }

        protected async Task RefreshFromChildControl()
        {
            await Task.Delay(0);

            //// Load data
            //await GetAll();

            //// Invoke StateHasChange
            //await InvokeAsync(StateHasChanged);
        }

        protected async Task GetAll()
        {
            await Task.Delay(0);


        }

        protected async Task DeleteHandler(GridCommandEventArgs args)
        {
            await Task.Delay(0);


        }

        protected async Task UpdateHandler(GridCommandEventArgs args)
        {
            await Task.Delay(0);


        }

        protected void Notify(string theme, string text)
        {
            var parameter = new Dictionary<string, string>
            {
                { "theme", theme },
                { "text", text }
            };
            NotifyParent.InvokeAsync(parameter);
        }

        public async void NotifyFromChild(Dictionary<string, string> parameters)
        {
            await Task.Delay(0);
            await NotifyParent.InvokeAsync(parameters);
        }
    }
}
