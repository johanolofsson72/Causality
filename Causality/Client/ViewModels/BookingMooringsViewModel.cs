using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Causality.Client.Services;
using Causality.Client.Shared;
using Causality.Shared.Data;
using Causality.Shared.Models;
using Ipify;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Telerik.Blazor.Components;

namespace Causality.Client.ViewModels
{
    public class BookingMooringsViewModel : ComponentBase, ICausalityViewModel, IDisposable
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

        [Inject] Services.CauseService CauseManager { get; set; }
        [Inject] Services.ClassService ClassManager { get; set; }
        [Inject] Services.MetaService MetaManager { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        protected bool IsMedium = false;
        protected bool IsSmall = false;
        protected string Title = "Moorings, Berths, Land Places, etc";
        protected List<BookingMooring> list = new();
        protected BookingMooring selectedItem = new();

        private int EventId { get; set; } = 1;
        private int ClassId { get; set; } = 0;

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

        protected async Task AutoCreate()
        {
            // Check with the user if this is OK
            if (!await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this moorings and mooring types?"))
                return;


            await Task.Run(() => {

            });
            await Task.Run(async () => await DeleteCausesAndClasses());

            await Task.Delay(1000);

            // Create a new Mooring type and Summer - Mooring - Water
            Class c = new()
            {
                EventId = EventId,
                Order = 1,
                Value = "Summer - Mooring - Water",
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            };
            await ClassManager.TryInsert(c, async (Class q, String s) =>
            {
                ClassId = q.Id;

                await CreateMooring(q.Id, "125", "11000", "3500", "2000");
                await CreateMooring(q.Id, "126", "11000", "3200", "2000");
                await CreateMooring(q.Id, "127", "11000", "3080", "2000");
                await CreateMooring(q.Id, "128", "11000", "2960", "2000");
                await CreateMooring(q.Id, "129", "11000", "3030", "2000");

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

            // Create a new Mooring type and Summer - Mooring - Land - Yard
            c = new()
            {
                EventId = EventId,
                Order = 2,
                Value = "Summer - Mooring - Land - Yard",
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            };
            await ClassManager.TryInsert(c, (Class q, String s) =>
            {
                Notify("info", "1 Mooring Type Created");

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

            // Create a new Mooring type and Summer - Mooring - Land - Cabin
            c = new()
            {
                EventId = EventId,
                Order = 3,
                Value = "Summer - Mooring - Land - Cabin",
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            };
            await ClassManager.TryInsert(c, (Class q, String s) =>
            {
                Notify("info", "1 Mooring Type Created");

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

            // Create a new Mooring type and Winter - Mooring - Water - Yboom
            c = new()
            {
                EventId = EventId,
                Order = 4,
                Value = "Winter - Mooring - Water - Yboom",
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            };
            await ClassManager.TryInsert(c, (Class q, String s) =>
            {
                Notify("info", "1 Mooring Type Created");

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

            // Create a new Mooring type and Winter - Mooring - Land - Yard
            c = new()
            {
                EventId = EventId,
                Order = 5,
                Value = "Winter - Mooring - Land - Yard",
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            };
            await ClassManager.TryInsert(c, (Class q, String s) =>
            {
                Notify("info", "1 Mooring Type Created");

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

            // Create a new Mooring type and Winter - Mooring - Land - Cabin
            c = new()
            {
                EventId = EventId,
                Order = 6,
                Value = "Winter - Mooring - Land - Cabin",
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            };
            await ClassManager.TryInsert(c, (Class q, String s) =>
            {
                Notify("info", "1 Mooring Type Created");

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);


            await Task.Delay(1000);

            // Load data
            await GetAll();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        private async Task DeleteCausesAndClasses()
        {
            // Delete all Causes with Metas
            await CauseManager.TryGet(c => c.EventId == EventId, "Id", true, "", async (IEnumerable<Cause> ca, String s) =>
            {
                List<Int32> _causes = new();
                List<Int32> _classes = new();
                foreach (var cause in ca)
                {
                    await CauseManager.TryDelete(cause.Id, null, null, StateProvider);
                }

                // Delete all Classes
                await ClassManager.TryGet(c => c.EventId == EventId, "Id", true, "", async (IEnumerable<Class> cl, String s) =>
                {
                    foreach (var item in cl)
                    {
                        await ClassManager.TryDelete(item.Id, null, null, StateProvider);
                    }

                }, null, StateProvider);

                Notify("info","All Causes and Classes is deleted!");

            }, null, StateProvider);
        }

        private async Task CreateMooring(int MooringType, string number, string length, string width, string depth)
        {
            // Create new Causes, ca 5 stycken med olika storlekar...
            Cause cause = new()
            {
                EventId = EventId,
                ClassId = MooringType,
                Order = 0,
                Value = number,
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            };
            await CauseManager.TryInsert(cause, async (Cause d, String s) =>
            {
                await Task.Delay(0);

                Notify("success", s);

                // lägg till alla meta fält...
                var LengthParameter = new Meta
                {
                    CauseId = d.Id,
                    ClassId = MooringType,
                    EffectId = 0,
                    EventId = EventId,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = 0,
                    Key = "Length",
                    Value = length,
                    UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
                };
                await MetaManager.TryInsert(LengthParameter, async (Meta m, String s) => { await Task.Delay(0); Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var WidthParameter = new Meta
                {
                    CauseId = d.Id,
                    ClassId = MooringType,
                    EffectId = 0,
                    EventId = EventId,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = 0,
                    Key = "Width",
                    Value = width,
                    UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
                };
                await MetaManager.TryInsert(WidthParameter, async (Meta m, String s) => { await Task.Delay(0); Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);

                var DepthParameter = new Meta
                {
                    CauseId = d.Id,
                    ClassId = MooringType,
                    EffectId = 0,
                    EventId = EventId,
                    ExcludeId = 0,
                    ProcessId = 0,
                    StateId = 0,
                    ResultId = 0,
                    UserId = 0,
                    Key = "Depth",
                    Value = depth,
                    UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
                };
                await MetaManager.TryInsert(DepthParameter, async (Meta m, String s) => { await Task.Delay(0); Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);


            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
        }

        private async Task DeleteOldMooringData()
        {
            List<Int32> _causes = new();
            List<Int32> _classes = new();

            // Delete all Causes with Metas
            await CauseManager.TryGet(c => c.EventId == EventId, "Id", true, "", (IEnumerable<Cause> ca, String s) =>
            {
                foreach (var cause in ca)
                {
                    _causes.Add(cause.Id);
                }

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

            // Delete all Classes
            await ClassManager.TryGet(c => c.EventId == EventId, "Id", true, "", (IEnumerable<Class> cl, String s) =>
            {
                foreach (var item in cl)
                {
                    _classes.Add(item.Id);
                }

            }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);

            foreach (var item in _causes)
            {
                await CauseManager.TryDelete(item, (String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
            }

            foreach (var item in _classes)
            {
                await ClassManager.TryDelete(item, (String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e + " " + s); }, StateProvider);
            }
        }

        protected async Task GetAll()
        {
            await CauseManager.TryGet(c => c.EventId == EventId, "Value", true, "Metas", GetAllSuccess, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
        }

        private async void GetAllSuccess(IEnumerable<Cause> causes, String s)
        {
            Notify("info", s);

            List<BookingMooring> _list = new();

            foreach (var u in causes)
            {
                BookingMooring bookingMooring = await PopulateMooring(u.Id, u.ClassId, u.Value, u.UpdatedDate, u.Metas);
                _list.Add(bookingMooring);
            }

            list = _list.OrderBy(x => x.Type).ToList();

            // Invoke StateHasChange
            await InvokeAsync(StateHasChanged);
        }

        private async Task<BookingMooring> PopulateMooring(int id, int classId, string value, string date, IEnumerable<Meta> metas)
        {
            return new BookingMooring()
            {
                Id = id,
                Name = value,
                Type = await GetMooringTypeById(classId),
                Length = "length".GetPropertyValueAsInt32(metas),
                Width = "width".GetPropertyValueAsInt32(metas),
                Depth = "depth".GetPropertyValueAsInt32(metas),
                UpdatedDate = date.ToDateTime()
            };
        }

        private async Task<string> GetMooringTypeById(int classId)
        {
            string ret = "missing";
            await ClassManager.TryGetById(classId, "", (Class cc, String s) =>
            {
                ret = cc.Value;

            }, (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
            return ret;
        }

        protected async Task UpdateHandler(GridCommandEventArgs args)
        {
            // Get the reference
            selectedItem = (BookingMooring)args.Item;

            var CauseId = selectedItem.Id;
            var Name = selectedItem.Name;
            var Length = selectedItem.Length;
            var Width = selectedItem.Width;
            var Depth = selectedItem.Depth;
            var UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");

            await CauseManager.TryGetById(CauseId, "Metas", async (Cause c, String s) =>
            {
                // Notify
                Notify("success", s);

                c.Value = Name;
                c.UpdatedDate = UpdatedDate;

                foreach (var item in c.Metas)
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
                        await MetaManager.TryUpdate(item, (Meta m, String s) => { Notify("success", s); }, (Exception e, String s) => { Notify("error", e.ToString() + " " + s); }, StateProvider);
                    }
                }

                await CauseManager.TryUpdate(c, async (Cause u, String s) =>
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
