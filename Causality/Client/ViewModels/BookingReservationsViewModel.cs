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
using System.Globalization;
using Causality.Shared.Data;

namespace Causality.Client.ViewModels
{
    public class BookingReservationsViewModel : ComponentBase, ICausalityViewModel, IDisposable
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
        [Inject] Services.ResultService ResultManager { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "Reservations";
        protected List<BookingReservation> list = new();
        protected BookingReservation selectedItem;
        protected List<string> BookingMooringData = new();
        protected List<string> BookingBoatData = new();
        protected bool BoatDropDownLoaded { get; set; } = false;
        protected bool MooringDropDownLoaded { get; set; } = false;

        public int EventId { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            list = new();
            selectedItem = null;
            BoatDropDownLoaded = false;
            MooringDropDownLoaded = false;
            BookingMooringData = new();
            BookingBoatData = new();

            // Load data
            await GetAll();

            // Get all Boats in Queue
            await LoadBoatDropDown();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadBoatDropDown()
        {
            // Get all Queue Items
            await StateManager.TryGet(s => s.EventId == EventId, "Id", true, "Metas", async (IEnumerable<State> states, String s) =>
            {
                await Task.Delay(0);
                List<BookingDropDownItem> bdd = new();
                foreach (var st in states)
                {
                    string Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Property.Search("boatname", st.Metas).ToString().ToLower()) +
                                  " " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Property.Search("boatlength", st.Metas).ToString().ToLower()) +
                                  " * " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Property.Search("boatwidth", st.Metas).ToString().ToLower()) +
                                  " * " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Property.Search("boatdepth", st.Metas).ToString().ToLower()) + 
                                  " in queue since: " + 
                                  Convert.ToDateTime(Property.Search("queueddate", st.Metas).ToString()).ToString("yyyy-MM-dd") + 
                                  " (" + st.ProcessId + ")";

                    BookingDropDownItem b = new()
                    {
                        Text = Name,
                        Value = Property.Search("queueddate", st.Metas).ToString()
                    };
                    bdd.Add(b);
                }
                bdd = bdd.OrderBy(x => x.Value).ToList();
                foreach (var item in bdd)
                {
                    BookingBoatData.Add(item.Text);
                }

                await Task.Delay(10);

                BoatDropDownLoaded = true;

                Notify("info", s);

                // Invoke StateHasChange
                await InvokeAsync(StateHasChanged);

            }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);

        }

        private async Task LoadMooringDropDown(int processId)
        {
            // Get the Boat
            await ProcessManager.TryGetById(processId, "Metas", async (Process p, String s) =>
            {
                int boatLength = Int32.Parse(Property.Search("length", p.Metas).ToString());
                int boatWidth = Int32.Parse(Property.Search("width", p.Metas).ToString());
                int boatDepth = Int32.Parse(Property.Search("depth", p.Metas).ToString());

                selectedItem.UserId = p.UserId;

                // Get all Moorings and check if they can hold the boat and if it's free !!!
                await CauseManager.TryGet(c => c.EventId == EventId && c.Results.All(r => r.CauseId != c.Id), "Id", true, "Metas", async (IEnumerable<Cause> causes, String s) =>
                {
                    await Task.Delay(0);
                    List<BookingMooringDropDownItem> bmdd = new();
                    foreach (var cause in causes)
                    {
                        int mooringLength = Int32.Parse(Property.Search("length", cause.Metas).ToString());
                        int mooringWidth = Int32.Parse(Property.Search("width", cause.Metas).ToString());
                        int mooringDepth = Int32.Parse(Property.Search("depth", cause.Metas).ToString());

                        // This should be, fit the boat to best fitted mooring, not to big, not to small
                        if (boatLength <= mooringLength && boatWidth <= mooringWidth && boatDepth <= mooringDepth)
                        {
                            string Name = cause.Value + " " + 
                                          mooringLength.ToString() + " * " +
                                          mooringWidth.ToString() + " * " +
                                          mooringDepth.ToString() +
                                          " (" + cause.Id.ToString() + ")";

                            BookingMooringDropDownItem bm = new()
                            {
                                Text = Name,
                                Value1 = mooringLength,
                                Value2 = mooringWidth,
                                Value3 = mooringDepth
                            };
                            bmdd.Add(bm);

                        }
                    }

                    bmdd = bmdd.OrderBy(x => x.Value2).ThenBy(x => x.Value3).ThenBy(x => x.Value1).ToList();

                    foreach (var item in bmdd)
                    {
                        BookingMooringData.Add(item.Text);
                    }



                    await Task.Delay(10);

                    BookingMooringData = BookingMooringData.ToList();

                    MooringDropDownLoaded = true;

                    Notify("info", s);

                    // Invoke StateHasChange
                    await InvokeAsync(StateHasChanged);

                }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);

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
            var processId = selectedItem.ProcessId;
            var eventId = EventId;
            var causeId = selectedItem.CauseId;
            var classId = selectedItem.ClassId;
            var userId = selectedItem.UserId;
            var value = DateTime.Now.ToString("yyyy-MM-dd");
            var updatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            Result result = new()
            {
                ProcessId = processId,
                EventId = eventId,
                CauseId = causeId,
                ClassId = classId,
                UserId = userId,
                Value = value,
                UpdatedDate = updatedDate
            };
            await ResultManager.TryInsert(result, async (Result r, String s) =>
            {
                // Delete from Queue
                await StateManager.TryGet(s => s.ProcessId == processId && s.EventId == eventId, "Id", true, "", async (IEnumerable<State> st, String o) =>
                {
                    foreach (var item in st)
                    {
                        await StateManager.TryDelete(item.Id, (string s) => { Notify("success", s); }, (Exception e, String r) => { Notify("error", e.ToString() + " " + r); }, StateProvider);
                    }

                }, (Exception e, String r) => { Notify("error", e.ToString() + " " + r); }, StateProvider);

                await Task.Delay(100);

                selectedItem = null;
                BoatDropDownLoaded = false;
                MooringDropDownLoaded = false;
                BookingMooringData = new();
                BookingBoatData = new();

                // Load data
                await GetAll();

                // Get all Boats in Queue
                await LoadBoatDropDown();

                // Invoke StateHasChange
                await InvokeAsync(StateHasChanged);

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

        }

        public async Task Cancel()
        {
            selectedItem = null;

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnSelectedItem(BookingReservation item)
        {
            if (item is null)
            {
                selectedItem = new();
                selectedItem.CustomerName = "";
                selectedItem.BoatName = "";
            }

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        public async Task BoatSelected(string boat)
        {
            if (boat.Contains("(") && boat.Contains(")"))
            {
                string Id = boat.Substring(boat.IndexOf("("));
                Id = Id.Replace("(", "").Replace(")", "");

                var processId = Int32.Parse(Id);
                if (processId > 0)
                {
                    selectedItem.ProcessId = processId;
                    selectedItem.BoatName = boat;
                    BookingMooringData = new();

                    // Get all Moorings
                    await LoadMooringDropDown(processId);
                }
            }

            await Task.Delay(100);

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        public async Task MooringSelected(string mooring)
        {
            if (mooring.Contains("(") && mooring.Contains(")"))
            {
                string Id = mooring.Substring(mooring.IndexOf("("));
                Id = Id.Replace("(", "").Replace(")", "");

                var causeId = Int32.Parse(Id);
                if (causeId > 0)
                {
                    await CauseManager.TryGetById(causeId, "", async (Cause c, String s) =>
                    {
                        await Task.Delay(0);
                        selectedItem.ClassId = c.ClassId;
                        selectedItem.CauseId = causeId;
                        selectedItem.MooringName = mooring;

                    }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
                }
            }

            await Task.Delay(100);

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task GetAll()
        {
            // Get all Reservations
            await ResultManager.TryGet(s => s.EventId == EventId, "Id", true, "Metas", async (IEnumerable<Result> r, String s) =>
            {
                await Task.Delay(0);
                List<BookingReservation> _list = new();
                foreach (var result in r)
                {
                    var bookingReservation = new BookingReservation()
                    {
                        Id = result.Id,
                        EventId = result.EventId,
                        ProcessId = result.ProcessId,
                        CauseId = result.CauseId,
                        ClassId = result.ClassId,
                        UserId = result.UserId,
                        CustomerName = await GetCustomerName(result.UserId),
                        BoatName = await GetBoatName(result.ProcessId),
                        MooringName = await GetMooringName(result.CauseId),
                        ReservedDate = Convert.ToDateTime(result.Value),
                        UpdatedDate = Convert.ToDateTime(result.UpdatedDate)
                    };
                    _list.Add(bookingReservation);
                }

                list = _list.OrderBy(x => x.MooringName).ToList();

                Notify("info", s);

            }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);

        }

        private async Task<string> GetCustomerName(int userId)
        {
            string ret = "";
            await UserManager.TryGetById(userId, "Metas", (User u, String s) =>
            {
                ret = Property.Search("firstname", u.Metas).ToString() + " " +
                      Property.Search("lastname", u.Metas).ToString();
            
            }, (Exception e, String r) => { Notify("error", e.ToString() + " " + r); }, StateProvider);
            return ret;
        }

        private async Task<string> GetBoatName(int processId)
        {
            string ret = "";
            await ProcessManager.TryGetById(processId, "", (Process p, String s) =>
            {
                ret = p.Value;

            }, (Exception e, String r) => { Notify("error", e.ToString() + " " + r); }, StateProvider);
            return ret;
        }

        private async Task<string> GetMooringName(int causeId)
        {
            string ret = "";
            await CauseManager.TryGetById(causeId, "", (Cause c, String s) =>
            {
                ret = c.Value;

            }, (Exception e, String r) => { Notify("error", e.ToString() + " " + r); }, StateProvider);
            return ret;
        }

        protected async Task DeleteHandler(GridCommandEventArgs args)
        {
            // Get the reference
            var Item = (BookingReservation)args.Item;

            // Ask the user
            if (!await JSRuntime.InvokeAsync<bool>("confirm", $"Are you sure you want to delete this reservation?"))
                return;

            // Delete all objects
            await ResultManager.TryDelete(Item.Id, (string s) => { Notify("success", s); }, (Exception e, String r) => { Notify("error", e.ToString() + " " + r); }, StateProvider);

            await Task.Delay(10);

            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
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
