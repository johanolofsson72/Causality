using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using BlazorOnlineState;
using Blazored.LocalStorage;

namespace Causality.Client.Services
{
    public class ApplicationState
    {
        // FormState properties...
        [JsonProperty] public string FormState { get; set; } = "";

        // System defined...
        [JsonProperty] public bool UseIndexedDB { get; private set; } = false;
        [JsonProperty] public bool OfflineMode { get; private set; } = false; 
        [JsonProperty] public bool WarmedUp { get; private set; } = false;
        [JsonProperty] public int TimeToLiveInSeconds { get; set; } = 30;
        [JsonProperty] public DateTime LastAccessed { get; set; } = DateTime.Now;

        public void UpdateWarmedUp(ComponentBase Source, bool warmedup)
        {
            this.WarmedUp = warmedup;
            NotifyStateChanged(Source, "WarmedUp");
        }
        public void UpdateOfflineMode(ComponentBase Source, bool enable)
        {
            this.OfflineMode = enable;
            if (!this.OfflineMode)
            {
                this.UseIndexedDB = false;
                NotifyStateChanged(Source, "UseIndexedDB");
            }
            NotifyStateChanged(Source, "OfflineMode");
        }
        public void UpdateUseIndexedDB(ComponentBase Source, bool use)
        {
            this.UseIndexedDB = use;
            NotifyStateChanged(Source, "UseIndexedDB");
        }
        public void UpdateTimeToLiveInSeconds(ComponentBase Source, int seconds)
        {
            this.TimeToLiveInSeconds = seconds;
            NotifyStateChanged(Source, "TimeToLiveInSeconds");
        }

        public event Action<ComponentBase, string> StateChanged;

        private void NotifyStateChanged(ComponentBase Source, string Property) => StateChanged?.Invoke(Source, Property);

    }
}
