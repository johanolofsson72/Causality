using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Causality.Client.Services;
using Causality.Client.Shared;
using Causality.Shared.Data;
using Causality.Shared.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace Causality.Client.ViewModels
{
    public class AppUserViewModel : ComponentBase, ICausalityViewModel, IDisposable
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

        [Parameter] public Int32 UserId { get; set; } = 0;

        [Inject] Services.UserService userService { get; set; }
        [Inject] Services.ClassService classService { get; set; }
        [Inject] Services.EffectService effectService { get; set; }
        [Inject] Services.CauseService causeService { get; set; }

        protected AppUser currentUser;

        protected async override Task OnParametersSetAsync()
        {
            if (UserId > 0)
            {
                await GetAppUser();
            }
        }

        private async Task GetAppUser()
        {
            DateTime executeTimer = DateTime.Now;

            User _user = new();
            await userService.TryGetById(UserId, "Exclude,Meta",  async (User u, string s) => 
            {
                await Task.Delay(0);
                _user = u; 

            }, (Exception e, string s) => { }, StateProvider);

            currentUser = new();
            currentUser.Id = _user.Id;
            currentUser.Name = _user.Name;
            currentUser.Metas = _user.Metas.ToList<Meta>();

            List<Effect> _effets = new();
            await effectService.TryGet(u => u.UserId == UserId, "CauseId", true, (IEnumerable<Effect> e, string s) => { _effets = e.ToList(); }, (Exception e, string s) => { }, StateProvider);
            currentUser.Interactions = new();

            foreach (var item in _effets)
            {
                AppUser.Interaction cce = new();

                await classService.TryGetById(item.ClassId, "", (Class c, string s) => { cce.Class = c.Value; }, (Exception e, string s) => { }, StateProvider);
                await causeService.TryGetById((Int32)item.CauseId, "", (Cause c, string s) => { cce.Cause = c.Value; }, (Exception e, string s) => { }, StateProvider);

                cce.Effect = item.Value;
                currentUser.Interactions.Add(cce);
            }

            // Loopa alla excludes som denna användaren har
            currentUser.ExcludedInteractions = new();
            foreach (var item in _user.Excludes)
            {
                AppUser.ExcludedInteraction ei = new();

                await causeService.TryGetById((Int32)item.CauseId, "", (Cause c, string s) => { ei.Cause = c.Value; }, (Exception e, string s) => { }, StateProvider);

                currentUser.ExcludedInteractions.Add(ei);
            }


            currentUser.ExecutionTime = (DateTime.Now.Subtract(executeTimer).TotalMilliseconds).ToString();
            await InvokeAsync(StateHasChanged);
        }

    }
}
