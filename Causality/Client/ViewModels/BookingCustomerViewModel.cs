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

namespace Causality.Client.ViewModels
{
    public class BookingCustomerViewModel : ComponentBase, ICausalityViewModel, IDisposable
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

        protected String Title = "New Customers";
        protected List<User> list;
        protected User selectedItem;

        protected override async Task OnInitializedAsync()
        {
            await Task.Delay(0);
            GetAll();
        }

        protected async Task RefreshFromChildControl()
        {
            await Task.Delay(0);
            GetAll();
        }

        protected async Task Delete(Int32 Id)
        {
            await UserManager.TryDelete(Id, (String s) => { GetAll(); Notify("success", s); }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Update()
        {
            await UserManager.TryUpdate(selectedItem, (User m, String s) => { GetAll(); Notify("success", s); }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Add()
        {
            var CustomerId = 0;
            var UID = new Guid().ToString();
            var IP = await IpifyIp.GetPublicIpAsync();
            var Name = "new_customer";
            var Email = "jool@me.com";
            var UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            var Firstname = "Johan";
            var Lastname = "Olofsson";
            var Address = "Droppemålavägen 14";
            var Postalcode = "37273";
            var City = "Ronneby";
            var Country = "Sverige";
            var Regnumber = "DDU001";
            var Phone = "+46709161669";

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
                    CauseId = 0, ClassId = 0, EffectId = 0, EventId = 0, ExcludeId = 0, ProcessId = 0, StateId = 0, ResultId = 0,
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
                    Key = "Phone",
                    Value = Phone,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(PhoneParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                Notify("success", s); 
            
            }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);

            // Get the customer with all its meta...
            await UserManager.TryGetById(CustomerId, "Meta", (User u, String s) => { list.Add(u); Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

            await InvokeAsync(StateHasChanged);
        }

        protected async Task Edit(Int32 Id)
        {
            await UserManager.TryGetById(Id, "", (User m, String s) => { selectedItem = m; Notify("info", s); }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
        }

        protected async Task Search(ChangeEventArgs args)
        {
            if (args.Value?.ToString().Length > 0)
            {
                await UserManager.TryGet(u => u.Name.ToLower().Contains(args.Value.ToString()), "Id", true, "", (IEnumerable<User> m, String s) => { list = m.ToList(); selectedItem = null; Notify("info", s); }, (Exception e, String r) => { list = null; selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            }
            else
            {
                GetAll();
            }
        }

        protected async void GetAll()
        {
            await UserManager.TryGet(u => u.Id > 0, "Id", true, "", (IEnumerable<User> m, String s) => { list = m.ToList(); selectedItem = null; Notify("info", s); }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
        }

        protected async Task Cancel()
        {
            await Task.Delay(0);
            selectedItem = null;
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
