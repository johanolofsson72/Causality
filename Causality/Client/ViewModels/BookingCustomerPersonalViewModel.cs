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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;
using Causality.Shared.Data;

namespace Causality.Client.ViewModels
{
    public class BookingCustomerPersonalViewModel : ComponentBase, ICausalityViewModel, IDisposable
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
        [Parameter] public EventCallback<Dictionary<string, string>> NotifyParent { get; set; }

        [Inject] Services.UserService UserManager { get; set; }
        [Inject] Services.MetaService MetaManager { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "Personal information";
        protected List<BookingCustomer> list = new();
        protected BookingCustomer SelectedBookingCustomer = new();

        protected override async Task OnInitializedAsync()
        {
            // Load data
            await Get();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task RefreshFromChildControl()
        {
            // Load data
            await Get();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Get()
        {
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

                SelectedBookingCustomer = bookingCustomer;

                Notify("info", s);

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
        }

        protected async Task DeleteHandler(BookingCustomer bookingCustomer)
        {
            if (!await JSRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete '{bookingCustomer.FirstName} {bookingCustomer.LastName}'?"))
                return;

            // Delete all objects
            await UserManager.TryDelete(bookingCustomer.Id, async (String s) =>
            {
                await Task.Delay(0);

                // Notify
                Notify("success", s);

                // Goto Customers
                NavigationManager.NavigateTo("bookingcustomers");

            }, (Exception e, String r) => { Notify("error", e.ToString() + " " + r); }, StateProvider);

        }

        protected async Task OnValidSubmitHandler()
        {
            // Get the reference
            var bookingCustomer = SelectedBookingCustomer;

            await UserManager.TryGetById(bookingCustomer.Id, "Metas", async (User u, String s) =>
            {
                // Notify
                Notify("success", s);

                u.Email = bookingCustomer.EmailAddress;
                u.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

                foreach (var item in u.Metas)
                {
                    bool update = false;
                    if (item.Key.ToLower().Equals("firstname", StringComparison.Ordinal))
                    {
                        item.Value = bookingCustomer.FirstName;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("lastname", StringComparison.Ordinal))
                    {
                        item.Value = bookingCustomer.LastName;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("address", StringComparison.Ordinal))
                    {
                        item.Value = bookingCustomer.Address;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("postalcode", StringComparison.Ordinal))
                    {
                        item.Value = bookingCustomer.PostalCode;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("city", StringComparison.Ordinal))
                    {
                        item.Value = bookingCustomer.City;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("country", StringComparison.Ordinal))
                    {
                        item.Value = bookingCustomer.Country;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("regnumber", StringComparison.Ordinal))
                    {
                        item.Value = bookingCustomer.RegNumber;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("phonenumber", StringComparison.Ordinal))
                    {
                        item.Value = bookingCustomer.PhoneNumber;
                        update = true;
                    }

                    if (update)
                    {
                        await MetaManager.TryUpdate(item, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);
                    }
                }

                await UserManager.TryUpdate(u, async (User u, String s) =>
                {
                    // Notify
                    Notify("success", s);

                    // Load data
                    await Get();

                    // Invoke StateHasChange
                    await InvokeAsync(StateHasChanged);

                }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

            }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

        }

        private void Notify(string theme, string text)
        {
            var parameter = new Dictionary<string, string>
            {
                { "theme", theme },
                { "text", text }
            };
            NotifyParent.InvokeAsync(parameter);
        }

    }
}
