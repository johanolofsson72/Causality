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
    public class BookingReservationCalculationViewModel : ComponentBase, ICausalityViewModel, IDisposable
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
        protected string Title = "Pre calculate the best mooring for all the boats in queue";
        protected List<BookingQueueItem> boatlist = new();
        protected List<BookingMooring> mooringlist = new();
        protected List<BookingReservation> prelist = new();
        protected bool BoatsAndMooringIsLoaded { get; set; } = false;

        public int EventId { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            boatlist = new();
            mooringlist = new();
            BoatsAndMooringIsLoaded = false;

            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Calculate()
        {
            // Deactivate the button
            BoatsAndMooringIsLoaded = false;

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);

            // Calculate
            await CalculateBoats();

            // Activate the button
            BoatsAndMooringIsLoaded = true;

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        private async Task CalculateBoats()
        {
            await Task.Delay(1000);
        }

        protected async Task RefreshFromChildControl()
        {
            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        private async Task<string> GetCustomerNameByUserId(int userId)
        {
            string ret = "";
            await UserManager.TryGetById(userId, "Metas", async (User u, String s) =>
            {
                await Task.Delay(0);
                ret = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Property.Search("firstname", u.Metas).ToString().ToLower()) + " " +
                      CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Property.Search("lastname", u.Metas).ToString().ToLower());

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
            return ret;
        }

        protected async Task GetAll()
        {
            await GetAllBoatInQueue();
            await GetAllFreeMoorings();
            BoatsAndMooringIsLoaded = true;
        }

        private async Task GetAllFreeMoorings()
        {
            // Get all free moorings
            await CauseManager.TryGet(c => c.EventId == EventId, "Value", true, "Metas", async (IEnumerable<Cause> causes, String s) =>
            {
                await Task.Delay(0);
                List<BookingMooring> _list = new();
                foreach (var u in causes)
                {
                    string type = "missing";
                    await ClassManager.TryGetById(u.ClassId, "", (Class cc, String s) =>
                    {
                        type = cc.Value;

                    }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

                    var bookingMooring = new BookingMooring()
                    {
                        Id = u.Id,
                        Name = u.Value,
                        Type = type,
                        Length = Int32.Parse(Property.Search("length", u.Metas).ToString()),
                        Width = Int32.Parse(Property.Search("width", u.Metas).ToString()),
                        Depth = Int32.Parse(Property.Search("depth", u.Metas).ToString()),
                        UpdatedDate = Convert.ToDateTime(u.UpdatedDate)
                    };
                    _list.Add(bookingMooring);
                }

                mooringlist = _list.OrderBy(x => x.Type).ToList();

                Notify("info", s);

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
        }

        private async Task GetAllBoatInQueue()
        {
            // Get all boats in queue without mooring
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
                        CustomerName = await GetCustomerNameByUserId(st.UserId),
                        BoatName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Property.Search("boatname", st.Metas).ToString().ToLower()),
                        BoatLength = Int32.Parse(Property.Search("boatlength", st.Metas).ToString()),
                        BoatWidth = Int32.Parse(Property.Search("boatwidth", st.Metas).ToString()),
                        BoatDepth = Int32.Parse(Property.Search("boatdepth", st.Metas).ToString()),
                        Comment = st.Value,
                        QueuedDate = Convert.ToDateTime(Property.Search("queueddate", st.Metas).ToString()),
                        UpdatedDate = Convert.ToDateTime(st.UpdatedDate)
                    };
                    _list.Add(bookingQueueItem);
                }

                boatlist = _list.OrderBy(x => x.QueuedDate).ToList();

                Notify("info", s);

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
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
