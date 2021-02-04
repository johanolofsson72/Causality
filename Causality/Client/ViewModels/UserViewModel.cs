using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Causality.Client.Services;
using Causality.Client.Shared;
using Causality.Shared.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace Causality.Client.ViewModels
{
    public class UserViewModel : ComponentBase, ICausalityViewModel, IDisposable
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

        [Parameter] public Int32 EventId { get; set; } = 0;

        [Parameter] public String EventKey { get; set; } = "";

        [Parameter] public EventCallback OnAdded { get; set; }

        [Parameter] public EventCallback<Dictionary<string, string>> NotifyParent { get; set; }

        [Inject] Services.UserService dataService { get; set; }

        protected String Title = "User";
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
            await dataService.TryDelete(Id, (String s) => { GetAll(); Notify("success", s); }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Update()
        {
            await dataService.TryUpdate(selectedItem, (User m, String s) => { GetAll(); Notify("success", s); }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Add()
        {
            var item = new User();
            item.UID = new Guid().ToString();
            item.IP = "127.0.0.1";
            item.Name = "Namn";
            item.Email = "Epost";
            item.UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            await dataService.TryInsert(item, (User m, String s) => { list.Add(m); Notify("success", s); }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Edit(Int32 Id)
        {
            await dataService.TryGetById(Id, "", (User m, String s) => { selectedItem = m; Notify("info", s); }, (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
        }

        protected async Task Search(ChangeEventArgs args)
        {
            if (args.Value?.ToString().Length > 0)
            {
                await dataService.TryGet(u => u.Name.ToLower().Contains(args.Value.ToString()), "Id", true, "", (IEnumerable<User> m, String s) => { list = m.ToList(); selectedItem = null; Notify("info", s); }, (Exception e, String r) => { list = null; selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            }
            else
            {
                GetAll();
            }
        }

        protected async void GetAll()
        {
            await dataService.TryGet(u => u.Id > 0, "Id", true, "", (IEnumerable<User> m, String s) => { list = m.ToList(); selectedItem = null; Notify("info", s); }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
        }

        protected async Task Cancel()
        {
            await Task.Delay(0);
            selectedItem = null;
        }

        protected void Notify(string theme, string text)
        {
            var parameter = new Dictionary<string, string>();
            parameter.Add("theme", theme);
            parameter.Add("text", text);
            NotifyParent.InvokeAsync(parameter);
        }

        public async void NotifyFromChild(Dictionary<string, string> parameters)
        {
            await Task.Delay(0);
            await NotifyParent.InvokeAsync(parameters);
        }
    }
}
