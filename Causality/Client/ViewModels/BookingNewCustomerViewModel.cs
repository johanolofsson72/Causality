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
    public class BookingNewCustomerViewModel : ComponentBase, ICausalityViewModel, IDisposable
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

        [Parameter] public EventCallback OnAdded { get; set; }
        [Parameter] public EventCallback<Dictionary<string, string>> NotifyParent { get; set; }

        [Inject] Services.UserService UserManager { get; set; }
        [Inject] Services.MetaService MetaManager { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "New Customers";
        protected List<BookingCustomer> list = new();
        protected BookingCustomer selectedItem = new();

        protected override async Task OnInitializedAsync()
        {
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

        protected async Task ApproveHandler(BookingCustomer item)
        {
            await UserManager.TryGetById(item.Id, "", async (User u, String s) =>
            {
                u.Name = "approved_customer";
                u.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

                await UserManager.TryUpdate(u, async (User u, String s) =>
                {
                    // Load data
                    await GetAll();

                    // Notify
                    Notify("success", s);

                    // Invoke StateHasChange
                    await InvokeAsync(StateHasChanged);

                }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

            }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

        }

        protected async Task GetAll()
        {
            await UserManager.TryGet(u => u.Id > 0 && u.Name == "new_customer", "Id", true, "Metas", async (IEnumerable<User> users, String s) =>
            {
                await Task.Delay(0);
                List<BookingCustomer> _list = new();
                foreach (var u in users)
                {
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
                    _list.Add(bookingCustomer);
                }

                list = _list.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToList();

                selectedItem = null;
                Notify("info", s);

            }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
        }

        protected async Task DeleteHandler(GridCommandEventArgs args)
        {
            // Get the reference
            selectedItem = (BookingCustomer)args.Item;

            if (!await JSRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete '{selectedItem.FirstName} {selectedItem.LastName}'?"))
                return;

            // Delete all objects
            await UserManager.TryDelete(selectedItem.Id, async (String s) =>
            {
                // Load data
                await GetAll();

                // Notify
                Notify("success", s);

                // Invoke StateHasChange
                await InvokeAsync(StateHasChanged);

            }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

        }

        protected async Task CreateHandler(GridCommandEventArgs args)
        {
            // Get the reference
            selectedItem = (BookingCustomer)args.Item;

            var CustomerId = 0;
            var UID = new Guid().ToString();
            var IP = "_";// await IpifyIp.GetPublicIpAsync();
            var Name = "new_customer";
            var Email = selectedItem.EmailAddress;
            var UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            var Firstname = selectedItem.FirstName;
            var Lastname = selectedItem.LastName;
            var Address = selectedItem.Address;
            var Postalcode = selectedItem.PostalCode;
            var City = selectedItem.City;
            var Country = selectedItem.Country;
            var Regnumber = selectedItem.RegNumber;
            var Phone = selectedItem.PhoneNumber;

            var item = new User
            {
                UID = UID,
                IP = IP,
                Name = Name,
                Email = Email,
                UpdatedDate = UpdatedDate
            };
            await UserManager.TryInsert(item, async (User c, String s) =>
            {
                CustomerId = c.Id;

                // lägg till alla meta fält...
                var FirstnameParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = c.Id,
                    Key = "Firstname",
                    Value = Firstname,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(FirstnameParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var LastnameParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = c.Id,
                    Key = "Lastname",
                    Value = Lastname,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(LastnameParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var AddressParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = c.Id,
                    Key = "Address",
                    Value = Address,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(AddressParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var PostalcodeParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = c.Id,
                    Key = "Postalcode",
                    Value = Postalcode,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(PostalcodeParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var CityParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = c.Id,
                    Key = "City",
                    Value = City,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(CityParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var CountryParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = c.Id,
                    Key = "Country",
                    Value = Country,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(CountryParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var RegnumberParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = c.Id,
                    Key = "Regnumber",
                    Value = Regnumber,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(RegnumberParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var PhoneParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = 0,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = c.Id,
                    Key = "Phonenumber",
                    Value = Phone,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(PhoneParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                // Load data
                await GetAll();

                // Notify
                Notify("success", s);

                // Invoke StateHasChange
                await InvokeAsync(StateHasChanged);

            }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

        }

        protected async Task UpdateHandler(GridCommandEventArgs args)
        {
            // Get the reference
            selectedItem = (BookingCustomer)args.Item;

            var CustomerId = selectedItem.Id;
            var UID = selectedItem.Uid;
            var Email = selectedItem.EmailAddress;
            var UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            var Firstname = selectedItem.FirstName;
            var Lastname = selectedItem.LastName;
            var Address = selectedItem.Address;
            var Postalcode = selectedItem.PostalCode;
            var City = selectedItem.City;
            var Country = selectedItem.Country;
            var Regnumber = selectedItem.RegNumber;
            var Phone = selectedItem.PhoneNumber;

            await UserManager.TryGetById(CustomerId, "Metas", async (User u, String s) =>
            {
                // Notify
                Notify("success", s);

                u.Email = Email;
                u.UpdatedDate = UpdatedDate;

                foreach (var item in u.Metas)
                {
                    bool update = false;
                    if (item.Key.ToLower().Equals("firstname", StringComparison.Ordinal))
                    {
                        item.Value = Firstname;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("lastname", StringComparison.Ordinal))
                    {
                        item.Value = Lastname;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("address", StringComparison.Ordinal))
                    {
                        item.Value = Address;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("postalcode", StringComparison.Ordinal))
                    {
                        item.Value = Postalcode;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("city", StringComparison.Ordinal))
                    {
                        item.Value = City;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("country", StringComparison.Ordinal))
                    {
                        item.Value = Country;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("regnumber", StringComparison.Ordinal))
                    {
                        item.Value = Regnumber;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("phonenumber", StringComparison.Ordinal))
                    {
                        item.Value = Phone;
                        update = true;
                    }

                    if (update)
                    {
                        await MetaManager.TryUpdate(item, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);
                    }
                }

                await UserManager.TryUpdate(u, async (User u, String s) =>
                {
                    // Load data
                    await GetAll();

                    // Notify
                    Notify("success", s);

                    // Invoke StateHasChange
                    await InvokeAsync(StateHasChanged);

                }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

            }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

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
