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
        [Inject] Services.CauseService CauseManager { get; set; }
        [Inject] Services.ClassService ClassManager { get; set; }
        [Inject] Services.StateService StateManager { get; set; }
        [Inject] Services.ProcessService ProcessManager { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "Pre calculate the best mooring for all the boats in queue";
        protected List<BookingQueueItem> boatlist = new();
        protected List<BookingMooring> mooringlist = new();
        protected List<BookingReservation> prelist = new();

        public int EventId { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            prelist = new();
            boatlist = new();
            mooringlist = new();

            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        protected async Task CalculateBoats()
        {
            SortedDictionary<int, Int64> boats = new();
            SortedDictionary<int, Int64> moorings = new();

            foreach (var item in boatlist)
            {
                boats.Add(item.Id, ((item.BoatLength / 100) * (item.BoatWidth / 100) * (item.BoatDepth / 100)));
            }

            foreach (var item in mooringlist)
            {
                moorings.Add(item.Id, ((item.Length / 100) * (item.Width / 100) * (item.Depth / 100)));
            }

            //foreach (var item in boats.OrderBy(b => b.Value))
            //{
            //    Console.WriteLine(item);
            //}

            //foreach (var item in moorings.OrderBy(m => m.Value))
            //{
            //    Console.WriteLine(item);
            //}

            int i = 0;
            prelist = new();
            if (boats.Count <= moorings.Count)
            {
                foreach (var item in boats.OrderBy(b => b.Value))
                {
                    BookingReservation pl = new()
                    {
                        Id = i,
                        MooringName = await GetMooringName(moorings.OrderBy(b => b.Value).ElementAt(i).Key),
                        BoatName = await GetBoatName(item.Key),
                        CustomerName = await GetCustomerName(item.Key)
                    };
                    prelist.Add(pl);
                    i++;
                }
            }
            else
            {
                foreach (var item in moorings.OrderBy(b => b.Value))
                {
                    BookingReservation pl = new()
                    {
                        Id = i,
                        MooringName = await GetMooringName(item.Key),
                        BoatName = await GetBoatName(boats.OrderBy(b => b.Value).ElementAt(i).Key),
                        CustomerName = await GetCustomerName(boats.OrderBy(b => b.Value).ElementAt(i).Key)
                    };
                    prelist.Add(pl);
                    i++;
                }
            }

            prelist = prelist.ToList();

            Notify("info", $"Calculated {prelist.Count} boats");

            //[16, 3186]  Askeladden
            //[15, 12250] Wellcraft
            //[13, 15120] Snipa
            //[14, 26368] Storebro 34
            //[17, 30294] Storebro 31

            //[35, 30000] 129
            //[34, 63800] 128
            //[33, 66000] 127
            //[32, 70400] 126
            //[31, 77000] 125

        }

        private async Task<string> GetMooringName(int causeId)
        {
            string ret = "";
            await CauseManager.TryGetById(causeId, "", async (Cause c, String s) =>
            {
                ret = c.Value;

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
            return ret;
        }

        private async Task<string> GetBoatName(int stateId)
        {
            string ret = "";
            await ProcessManager.TryGet(p => p.EventId == EventId && p.States.All(r => r.Id == stateId), "Id",true, "", (IEnumerable<Process> processes, String s) =>
            {
                ret = processes.FirstOrDefault().Value;

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
            return ret;
        }

        private async Task<string> GetCustomerName(int stateId)
        {
            string ret = "";
            await UserManager.TryGet(p => p.States.Any(r => r.Id == stateId), "Id", true, "Metas", (IEnumerable<User> users, String s) =>
            {
                if (users.Any())
                {
                    ret = "firstname".GetPropertyValueAsString(users.FirstOrDefault().Metas).ToTitleCase() + 
                          " " +
                          "lastname".GetPropertyValueAsString(users.FirstOrDefault().Metas).ToTitleCase();
                }

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
            return ret;
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
            await UserManager.TryGetById(userId, "Metas", (User u, String s) =>
            {
                ret = "firstname".GetPropertyValueAsString(u.Metas).ToTitleCase() + " " +
                      "lastname".GetPropertyValueAsString(u.Metas).ToTitleCase();

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
            return ret;
        }

        protected async Task GetAll()
        {
            await GetAllBoatInQueue();
            await GetAllFreeMoorings();
        }

        private async Task GetAllFreeMoorings()
        {
            // Get all free moorings
            await CauseManager.TryGet(c => c.EventId == EventId, "Value", true, "Metas", async (IEnumerable<Cause> causes, String s) =>
            {
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
                        Length = "length".GetPropertyValueAsInt32(u.Metas),
                        Width = "width".GetPropertyValueAsInt32(u.Metas),
                        Depth = "depth".GetPropertyValueAsInt32(u.Metas),
                        UpdatedDate = u.UpdatedDate.ToDateTime()
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
                        BoatName = "boatname".GetPropertyValueAsString(st.Metas).ToTitleCase(),
                        BoatLength = "boatlength".GetPropertyValueAsInt32(st.Metas),
                        BoatWidth = "boatwidth".GetPropertyValueAsInt32(st.Metas),
                        BoatDepth = "boatdepth".GetPropertyValueAsInt32(st.Metas),
                        Comment = st.Value,
                        QueuedDate = "queueddate".GetPropertyValueAsDateTime(st.Metas),
                        UpdatedDate = st.UpdatedDate.ToDateTime()
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
