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
    public class BookingMooringTypesViewModel : ComponentBase, ICausalityViewModel, IDisposable
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

        [Inject] Services.ClassService ClassManager { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "Types (Moorings, Berths, Land Places, etc)";
        protected List<BookingMooringType> list = new();
        protected BookingMooringType BookingMooringType = new();
        private int EventId { get; set; } = 1;

        private static object SeachForProperty(string propertyName, IEnumerable<Meta> list)
        {
            var ret = "missing";
            try
            {
                foreach (var item in list)
                {
                    if (item.Key.ToLower().Equals(propertyName.ToLower()))
                    {
                        return item.Value;
                    }
                }
                return ret;
            }
            catch
            {
                return ret;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await Task.Delay(0);

            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task RefreshFromChildControl()
        {
            await Task.Delay(0);

            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task GetAll()
        {
            await ClassManager.TryGet(c => c.EventId == EventId, "Value", true, "", async (IEnumerable<Class> cc, String s) =>
            {
                await Task.Delay(0);
                List<BookingMooringType> _list = new();
                foreach (var item in cc)
                {
                    var bmt = new BookingMooringType()
                    {
                        Id = item.Id,
                        Name = item.Value,
                        UpdatedDate = Convert.ToDateTime(item.UpdatedDate)
                    };
                    _list.Add(bmt);
                }

                list = _list.OrderBy(x => x.Name).ToList();

                BookingMooringType = null;
                Notify("info", s);

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
        }

        protected async Task DeleteHandler(GridCommandEventArgs args)
        {
            // Get the reference
            BookingMooringType = (BookingMooringType)args.Item;

            if (!await JSRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete {BookingMooringType.Name}?"))
                return;

            await ClassManager.TryDelete(BookingMooringType.Id, (String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task CreateHandler(GridCommandEventArgs args)
        {
            // Get the reference
            BookingMooringType = (BookingMooringType)args.Item;

            // Create new Class
            Class c = new()
            {
                EventId = EventId,
                Order = 0,
                Value = BookingMooringType.Name,
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            };
            await ClassManager.TryInsert(c, (Class r, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);

        }

        protected async Task UpdateHandler(GridCommandEventArgs args)
        {
            // Get the reference
            BookingMooringType = (BookingMooringType)args.Item;

            var Id = BookingMooringType.Id;
            var Name = BookingMooringType.Name;
            var UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            await ClassManager.TryGetById(Id, "", async (Class p, String s) =>
            {
                // Notify
                Notify("success", s);

                p.Value = Name;
                p.UpdatedDate = UpdatedDate;

                await ClassManager.TryUpdate(p, async (Class pt, String s) =>
                {
                    // Load data
                    await GetAll();

                    // Notify
                    Notify("success", s);

                    // Invoke StateHasChange
                    await InvokeAsync(StateHasChanged);

                }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

            }, (Exception e, String r) => { BookingMooringType = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

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
