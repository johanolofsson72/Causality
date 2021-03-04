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
    public class BookingQueuesViewModel : ComponentBase, ICausalityViewModel, IDisposable
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
        [Inject] Services.CauseService CauseManager { get; set; }
        [Inject] Services.ClassService ClassManager { get; set; }
        [Inject] Services.StateService StateManager { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "Queues";
        protected List<BookingQueueItem> list = new();
        protected BookingQueueItem selectedItem = new();
        protected List<BookingCustomerDropDown> BookingCustomerData = new();
        public BookingCustomerDropDown BookingCustomerToEdit { get; set; } = new();
        public int EventId { get; set; } = 1;

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
            // Load data
            await GetAll();

            // Get all Customers
            await LoadCustomerDropDown();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadCustomerDropDown()
        {
            await UserManager.TryGet(u => u.Id > 0 && u.Name == "approved_customer", "Id", true, "Metas", async (IEnumerable<User> users, String s) =>
            {
                await Task.Delay(0);
                List<BookingCustomerDropDown> _list = new();
                foreach (var u in users)
                {
                    var bookingCustomer = new BookingCustomerDropDown()
                    {
                        Value = u.Id,
                        Text = SeachForProperty("firstname", u.Metas).ToString() + " " + SeachForProperty("lastname", u.Metas).ToString()
                    };
                    _list.Add(bookingCustomer);
                }

                BookingCustomerData = _list.OrderBy(x => x.Text).ToList();

                Notify("info", s);

            }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
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
            // Get all Queue Items
            await StateManager.TryGet(s => s.EventId == EventId, "Id", true, "Metas", async (IEnumerable<State> states, String s) =>
            {
                await Task.Delay(0);
                List<BookingQueueItem> _list = new();
                foreach (var st in states)
                {
                    var bookingQueueItem = new BookingQueueItem()
                    {
                        Id = st.Id,
                        EventId = EventId,
                        CauseId = st.CauseId,
                        ClassId = st.ClassId,
                        UserId = st.UserId,
                        BoatName = "", // SeachForProperty("boatname", st.Metas).ToString(),
                        BoatLength = 0, // Int32.Parse(SeachForProperty("boatlength", st.Metas).ToString()),
                        BoatWidth = 0, // Int32.Parse(SeachForProperty("boatwidth", st.Metas).ToString()),
                        BoatDepth = 0, //Int32.Parse(SeachForProperty("boatdepth", st.Metas).ToString()),
                        Comment = st.Value,
                        QueuedDate = DateTime.Now, // Convert.ToDateTime(SeachForProperty("queueddate", st.Metas).ToString()),
                        UpdatedDate = Convert.ToDateTime(st.UpdatedDate)
                    };
                    _list.Add(bookingQueueItem);
                }

                list = _list.OrderBy(x => x.QueuedDate).ToList();

                selectedItem = null;
                Notify("info", s);

            }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);

        }

        public void SetSelectedCustomer(string c)
        {
            string pop = "";
        }

        protected async Task DeleteHandler(GridCommandEventArgs args)
        {
            // Get the reference
            selectedItem = (BookingQueueItem)args.Item;

            if (!await JSRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete the queue item for '{selectedItem.BoatName} and {selectedItem.CustomerName}'?"))
                return;

            // Delete all objects
            await StateManager.TryDelete(selectedItem.Id, async (string s) =>
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
            selectedItem = (BookingQueueItem)args.Item;

            var stateId = 0;
            var eventId = EventId;
            var causeId = selectedItem.CauseId;
            var classId = selectedItem.ClassId;
            var userId = selectedItem.UserId;
            var BoatName = selectedItem.BoatName;
            var BoatLength = selectedItem.BoatLength;
            var BoatWidth = selectedItem.BoatWidth;
            var BoatDepth = selectedItem.BoatDepth;
            var Comment = selectedItem.Comment;
            var QueuedDate = selectedItem.UpdatedDate.ToString("yyyy-MM-dd hh:mm:ss");
            var UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            var item = new State
            {
                EventId = eventId,
                CauseId = causeId,
                ClassId = classId,
                UserId = userId,
                Value = Comment,
                UpdatedDate = UpdatedDate
            };
            await StateManager.TryInsert(item, async (State state, String s) =>
            {
                stateId = state.Id;

                // lägg till alla meta fält...
                var BoatNameParameter = new Meta
                {
                    CauseId = causeId,
                    ClassId = classId,
                    EffectId = 0,
                    EventId = eventId,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = stateId,
                    ResultId = 0,
                    UserId = userId,
                    Key = "BoatName",
                    Value = BoatName,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(BoatNameParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var BoatLengthParameter = new Meta
                {
                    CauseId = causeId,
                    ClassId = classId,
                    EffectId = 0,
                    EventId = eventId,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = stateId,
                    ResultId = 0,
                    UserId = userId,
                    Key = "BoatLength",
                    Value = BoatLength.ToString(),
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(BoatLengthParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var BoatWidthParameter = new Meta
                {
                    CauseId = causeId,
                    ClassId = classId,
                    EffectId = 0,
                    EventId = eventId,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = stateId,
                    ResultId = 0,
                    UserId = userId,
                    Key = "BoatWidth",
                    Value = BoatWidth.ToString(),
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(BoatWidthParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var BoatDepthParameter = new Meta
                {
                    CauseId = causeId,
                    ClassId = classId,
                    EffectId = 0,
                    EventId = eventId,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = stateId,
                    ResultId = 0,
                    UserId = userId,
                    Key = "BoatDepth",
                    Value = BoatDepth.ToString(),
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(BoatDepthParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var QueuedDateParameter = new Meta
                {
                    CauseId = causeId,
                    ClassId = classId,
                    EffectId = 0,
                    EventId = eventId,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = stateId,
                    ResultId = 0,
                    UserId = userId,
                    Key = "QueuedDate",
                    Value = QueuedDate,
                    UpdatedDate = UpdatedDate
                };
                await MetaManager.TryInsert(QueuedDateParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

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
            selectedItem = (BookingQueueItem)args.Item;

            var stateId = selectedItem.Id;
            var eventId = selectedItem.EventId;
            var causeId = selectedItem.CauseId;
            var classId = selectedItem.ClassId;
            var userId = selectedItem.UserId;
            var BoatName = selectedItem.BoatName;
            var BoatLength = selectedItem.BoatLength;
            var BoatWidth = selectedItem.BoatWidth;
            var BoatDepth = selectedItem.BoatDepth;
            var Comment = selectedItem.Comment;
            var QueuedDate = selectedItem.UpdatedDate.ToString("yyyy-MM-dd hh:mm:ss");
            var UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            await StateManager.TryGetById(stateId, "Metas", async (State u, String s) =>
            {
                // Notify
                Notify("success", s);

                u.Value = Comment;
                u.UpdatedDate = UpdatedDate;

                foreach (var item in u.Metas)
                {
                    bool update = false;
                    if (item.Key.ToLower().Equals("boatname", StringComparison.Ordinal))
                    {
                        item.Value = BoatName;
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("boatlength", StringComparison.Ordinal))
                    {
                        item.Value = BoatLength.ToString();
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("boatwidth", StringComparison.Ordinal))
                    {
                        item.Value = BoatWidth.ToString();
                        update = true;
                    }
                    else if (item.Key.ToLower().Equals("boatdepth", StringComparison.Ordinal))
                    {
                        item.Value = BoatDepth.ToString();
                        update = true;
                    }

                    if (update)
                    {
                        await MetaManager.TryUpdate(item, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);
                    }
                }

                await StateManager.TryUpdate(u, async (State u, String s) =>
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
