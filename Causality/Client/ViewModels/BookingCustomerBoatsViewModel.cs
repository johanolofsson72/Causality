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
using Causality.Shared.Data;

namespace Causality.Client.ViewModels
{
    public class BookingCustomerBoatsViewModel : ComponentBase, ICausalityViewModel, IDisposable
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
        [Inject] Services.ProcessService ProcessManager { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "Boats";
        protected List<BookingBoat> list = new();
        protected BookingBoat selectedItem = new();
        protected BookingCustomer selectedCustomer = new();
        private int EventId { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            // Load current user
            await UserManager.TryGetById(BookingCustomerId, "Metas", async (User u, String s) =>
            {
                await Task.Delay(0);

                var bookingCustomer = new BookingCustomer()
                {
                    Id = u.Id,
                    Uid = u.UID,
                    Status = u.Name,
                    EmailAddress = u.Email,
                    UpdatedDate = Convert.ToDateTime(u.UpdatedDate),
                    FirstName = Property.Search("firstname", u.Metas).ToString(),
                    LastName = Property.Search("lastname", u.Metas).ToString(),
                    Address = Property.Search("address", u.Metas).ToString(),
                    PostalCode = Property.Search("postalcode", u.Metas).ToString(),
                    City = Property.Search("city", u.Metas).ToString(),
                    Country = Property.Search("country", u.Metas).ToString(),
                    PhoneNumber = Property.Search("phonenumber", u.Metas).ToString(),
                    RegNumber = Property.Search("regnumber", u.Metas).ToString()
                };

                selectedCustomer = bookingCustomer;

                Title = selectedCustomer.FirstName + "´s boats";

            }, async (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task RefreshFromChildControl()
        {
            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task GetAll()
        {
            await ProcessManager.TryGet(p => p.UserId == BookingCustomerId && p.EventId == EventId, "Value", true, "Metas", async (IEnumerable<Process> p, String s) =>
            {
                await Task.Delay(0);
                List<BookingBoat> _list = new();
                foreach (var b in p)
                {
                    var bookingBoat = new BookingBoat()
                    {
                        Id = b.Id,
                        Type = b.Value,
                        Length = Int32.Parse(Property.Search("length", b.Metas).ToString()),
                        Width = Int32.Parse(Property.Search("width", b.Metas).ToString()),
                        Depth = Int32.Parse(Property.Search("depth", b.Metas).ToString())
                    };
                    _list.Add(bookingBoat);
                }

                list = _list.OrderBy(x => x.Type).ToList();

                selectedItem = null;
                Notify("info", s);

            }, async (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
        }

        protected async Task DeleteHandler(GridCommandEventArgs args)
        {
            // Get the reference
            selectedItem = (BookingBoat)args.Item;

            // Delete all objects
            await ProcessManager.TryDelete(selectedItem.Id, async (String s) =>
            {
                // Load data
                await GetAll();

                // Notify
                Notify("success", s);

                // Invoke StateHasChange
                await InvokeAsync(StateHasChanged);

            }, async (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

        }

        protected async Task UpdateHandler(GridCommandEventArgs args)
        {
            // Get the reference
            selectedItem = (BookingBoat)args.Item;

            var BoatId = selectedItem.Id;
            var Type = selectedItem.Type;
            var UserId = BookingCustomerId;
            var Length = selectedItem.Length;
            var Width = selectedItem.Width;
            var Depth = selectedItem.Depth;
            var UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            await ProcessManager.TryGetById(BoatId, "Metas", async (Process p, String s) =>
            {
                // Notify
                Notify("success", s);

                p.Value = Type;
                p.UpdatedDate = UpdatedDate;

                foreach (var item in p.Metas)
                {
                    bool update = false;
                    if (item.Key.ToLower().Equals("length", StringComparison.Ordinal))
                    {
                        item.Value = Length.ToString();
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("width", StringComparison.Ordinal))
                    {
                        item.Value = Width.ToString();
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("depth", StringComparison.Ordinal))
                    {
                        item.Value = Depth.ToString();
                        update = true;
                    }

                    if (update)
                    {
                        await MetaManager.TryUpdate(item, async (Meta m, String s) => { Notify("success", s); }, async (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);
                    }
                }

                await ProcessManager.TryUpdate(p, async (Process p, String s) =>
                {
                    // Load data
                    await GetAll();

                    // Notify
                    Notify("success", s);

                    // Invoke StateHasChange
                    await InvokeAsync(StateHasChanged);

                }, async (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

            }, async (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

        }

        protected async Task CreateHandler(GridCommandEventArgs args)
        {

            // Get the reference
            selectedItem = (BookingBoat)args.Item;

            var UserId = BookingCustomerId;
            var Length = selectedItem.Length;
            var Width = selectedItem.Width;
            var Depth = selectedItem.Depth;
            var UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            var item = new Process
            {
                EventId = EventId,
                UserId = UserId,
                Order = 1,
                Value = selectedItem.Type,
                UpdatedDate = UpdatedDate
            };
            await ProcessManager.TryInsert(item, async (Process p, String s) =>
            {
                var BoatId = p.Id;

                // lägg till alla meta fält...
                var LengthParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = BoatId,
                    StateId = 0,
                    ResultId = 0,
                    UserId = UserId,
                    Key = "Length",
                    Value = Length.ToString(),
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(LengthParameter, async (Meta m, String s) => { Notify("success", s); }, async (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var WidthParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = BoatId,
                    StateId = 0,
                    ResultId = 0,
                    UserId = UserId,
                    Key = "Width",
                    Value = Width.ToString(),
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(WidthParameter, async (Meta m, String s) => { Notify("success", s); }, async (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var DepthParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = BoatId,
                    StateId = 0,
                    ResultId = 0,
                    UserId = UserId,
                    Key = "Depth",
                    Value = Depth.ToString(),
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(DepthParameter, async (Meta m, String s) => { Notify("success", s); }, async (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                // Load data
                await GetAll();

                // Notify
                Notify("success", s);

                // Invoke StateHasChange
                await InvokeAsync(StateHasChanged);

            }, async (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

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
