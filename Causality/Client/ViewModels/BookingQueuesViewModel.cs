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
        [Inject] Services.ProcessService ProcessManager { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "Queues";
        protected List<BookingQueueItem> list = new();
        protected BookingQueueItem selectedItem;
        protected List<string> BookingCustomerData = new();
        protected List<string> BookingBoatData = new();

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
                foreach (var u in users)
                {
                    string Name = SeachForProperty("firstname", u.Metas).ToString() +
                                    " " +
                                    SeachForProperty("lastname", u.Metas).ToString() +
                                    " (" + u.Id + ")";

                    BookingCustomerData.Add(Name);
                }

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

        public async Task Save()
        {
            var eventId = EventId;
            var userId = selectedItem.UserId;
            var userName = selectedItem.CustomerName;
            var boatId = selectedItem.ProcessId;
            var boatName = selectedItem.BoatName;
            var boatLength = selectedItem.BoatLength;
            var boatWidth = selectedItem.BoatWidth;
            var boatDepth = selectedItem.BoatDepth;
            var comment = selectedItem.Comment;
            var queuedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            var updatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            State queue = new()
            {
                EventId = eventId,
                ProcessId = boatId,
                UserId = userId,
                Value = comment,
                UpdatedDate = updatedDate
            };
            await StateManager.TryInsert(queue, async (State st, String s) =>
            {
                // lägg till alla meta fält...
                var boatNameParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = st.EventId,
                    ExcludeId = 0,
                    ProcessId = st.ProcessId,
                    StateId = st.Id,
                    ResultId = 0,
                    UserId = st.UserId,
                    Key = "BoatName",
                    Value = boatName,
                    UpdatedDate = st.UpdatedDate
                };
                await MetaManager.TryInsert(boatNameParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var boatLengthParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = st.EventId,
                    ExcludeId = 0,
                    ProcessId = st.ProcessId,
                    StateId = st.Id,
                    ResultId = 0,
                    UserId = st.UserId,
                    Key = "BoatLength",
                    Value = boatLength.ToString(),
                    UpdatedDate = st.UpdatedDate
                };
                await MetaManager.TryInsert(boatLengthParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var boatWidthParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = st.EventId,
                    ExcludeId = 0,
                    ProcessId = st.ProcessId,
                    StateId = st.Id,
                    ResultId = 0,
                    UserId = st.UserId,
                    Key = "BoatWidth",
                    Value = boatWidth.ToString(),
                    UpdatedDate = st.UpdatedDate
                };
                await MetaManager.TryInsert(boatWidthParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var boatDepthParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = st.EventId,
                    ExcludeId = 0,
                    ProcessId = st.ProcessId,
                    StateId = st.Id,
                    ResultId = 0,
                    UserId = st.UserId,
                    Key = "BoatDepth",
                    Value = boatDepth.ToString(),
                    UpdatedDate = st.UpdatedDate
                };
                await MetaManager.TryInsert(boatDepthParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var queuedDateParameter = new Meta
                {
                    CauseId = 0,
                    ClassId = 0,
                    EffectId = 0,
                    EventId = st.EventId,
                    ExcludeId = 0,
                    ProcessId = st.ProcessId,
                    StateId = st.Id,
                    ResultId = 0,
                    UserId = st.UserId,
                    Key = "QueuedDate",
                    Value = queuedDate,
                    UpdatedDate = st.UpdatedDate
                };
                await MetaManager.TryInsert(queuedDateParameter, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                selectedItem = null;

                // Load data
                await GetAll();

                // Invoke StateHasChange
                await InvokeAsync(StateHasChanged);

            }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);

        }

        public async Task Cancel()
        {
            selectedItem = null;

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnSelectedItem(BookingQueueItem item)
        {
            if (item is null)
            {
                selectedItem = new();
                selectedItem.CustomerName = "";
                selectedItem.Comment = "";
            }

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        public async Task CustomerSelected(string customer)
        {
            if (customer.Contains("(") && customer.Contains(")"))
            {
                string Id = customer.Substring(customer.IndexOf("("));
                Id = Id.Replace("(", "").Replace(")", "");

                var userId = Int32.Parse(Id);
                if (userId > 0)
                {
                    selectedItem.UserId = userId;
                    selectedItem.CustomerName = customer;

                    await ProcessManager.TryGet(p => p.UserId == userId, "Id", true, "Metas", async (IEnumerable<Process> p, String s) =>
                    {
                        await Task.Delay(0);
                        BookingBoatData = new();
                        foreach (var b in p)
                        {
                            string boat = b.Value + " " + 
                                SeachForProperty("length", b.Metas).ToString() + "/" + 
                                SeachForProperty("width", b.Metas).ToString() + "/" + 
                                SeachForProperty("depth", b.Metas).ToString() + " (" + b.Id.ToString() + ")";

                            BookingBoatData.Add(boat);
                        }

                        Notify("info", "båten finns");

                        // Invoke StateHasChange
                        await InvokeAsync(StateHasChanged);

                    }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
                }
                else
                {
                    selectedItem.CustomerName = "";
                }
            }
            else
            {
                selectedItem.CustomerName = "";
            }
        }

        public async Task BoatSelected(string boat)
        {
            if (boat.Contains("(") && boat.Contains(")"))
            {
                string Id = boat.Substring(boat.IndexOf("("));
                Id = Id.Replace("(", "").Replace(")", "");

                var boatId = Int32.Parse(Id);
                if (boatId > 0)
                {
                    await ProcessManager.TryGetById(boatId, "Metas", async (Process p, String s) =>
                    {
                        await Task.Delay(0);

                        selectedItem.BoatName = p.Value;
                        selectedItem.ProcessId = p.Id;
                        selectedItem.BoatLength = Int32.Parse(SeachForProperty("length", p.Metas).ToString());
                        selectedItem.BoatWidth = Int32.Parse(SeachForProperty("width", p.Metas).ToString());
                        selectedItem.BoatDepth = Int32.Parse(SeachForProperty("depth", p.Metas).ToString());

                        Notify("info", "båten uppdaterad");

                        // Invoke StateHasChange
                        await InvokeAsync(StateHasChanged);

                    }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
                }
                    

            }
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
                        ProcessId = st.ProcessId,
                        UserId = st.UserId,
                        BoatName = SeachForProperty("boatname", st.Metas).ToString(),
                        BoatLength = Int32.Parse(SeachForProperty("boatlength", st.Metas).ToString()),
                        BoatWidth = Int32.Parse(SeachForProperty("boatwidth", st.Metas).ToString()),
                        BoatDepth = Int32.Parse(SeachForProperty("boatdepth", st.Metas).ToString()),
                        Comment = st.Value,
                        QueuedDate = Convert.ToDateTime(SeachForProperty("queueddate", st.Metas).ToString()),
                        UpdatedDate = Convert.ToDateTime(st.UpdatedDate)
                    };
                    _list.Add(bookingQueueItem);
                }

                list = _list.OrderBy(x => x.QueuedDate).ToList();

                selectedItem = null;
                Notify("info", s);

            }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);

        }

        protected async Task DeleteHandler(GridCommandEventArgs args)
        {
            // Get the reference
            var Item = (BookingQueueItem)args.Item;

            if (!await JSRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete the queue item for '{Item.BoatName} and {Item.CustomerName}'?"))
                return;

            // Delete all objects
            await StateManager.TryDelete(Item.Id, async (string s) =>
            {
                // Load data
                await GetAll();

                // Notify
                Notify("success", s);

                // Invoke StateHasChange
                await InvokeAsync(StateHasChanged);

            }, (Exception e, String r) => { Notify("error", e.ToString() + " " + r); }, StateProvider);


        }

        protected async Task UpdateHandler(GridCommandEventArgs args)
        {
            // Get the reference
            selectedItem = (BookingQueueItem)args.Item;

            var stateId = selectedItem.Id;
            var eventId = selectedItem.EventId;
            var processId = selectedItem.ProcessId;
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
