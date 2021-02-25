using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorOnlineState;
using Causality.Shared.Models;
using Causality.Shared.Data;
using TG.Blazor.IndexedDB;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Components;
using Causality.Client.Shared;
using Serialize.Linq.Serializers;
using System.Linq.Expressions;

/// <summary>
/// Can be copied when adding new service
/// Mark the prefix before "xxxxService" and replace and you are good to go
/// </summary>
namespace Causality.Client.Services
{
    public class EventService
    {
        Causality.Shared.Models.EventService.EventServiceClient _eventService;
        Causality.Shared.Models.ClassService.ClassServiceClient _classService;
        Causality.Shared.Models.EffectService.EffectServiceClient _effectService;
        Causality.Shared.Models.CauseService.CauseServiceClient _causeService;
        Causality.Shared.Models.ExcludeService.ExcludeServiceClient _excludeService;
        IndexedDBManager _indexedDBManager;
        OnlineStateService _onlineState;

        public EventService(Causality.Shared.Models.EventService.EventServiceClient eventService,
            Causality.Shared.Models.ClassService.ClassServiceClient classService,
            Causality.Shared.Models.EffectService.EffectServiceClient effectService,
            Causality.Shared.Models.CauseService.CauseServiceClient causeService,
            Causality.Shared.Models.ExcludeService.ExcludeServiceClient excludeService, 
            IndexedDBManager indexedDBManager, 
            OnlineStateService onlineState)
        {
            _eventService = eventService;
            _classService = classService;
            _effectService = effectService;
            _causeService = causeService;
            _excludeService = excludeService;
            _indexedDBManager = indexedDBManager;
            _onlineState = onlineState;
        }

        public async Task TryDelete(int id, Action<string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                if (await _onlineState.IsOnline())
                {
                    EventRequestDelete req = new() { Id = id };
                    EventResponseDelete ret = await _eventService.DeleteAsync(req);
                    if (!ret.Success)
                    {
                        throw new Exception(RequestCodes.FIVE_ZERO_ZERO);
                    }
                }

                if (state.AppState.UseIndexedDB)
                {
                    await _indexedDBManager.OpenDb();
                    await _indexedDBManager.ClearStore("Blobs");
                }

                onSuccess(RequestCodes.TWO_ZERO_ZERO);

            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        /// <summary>
        /// TryGet, Includes (Class, Cause, Effect, Exclude, Meta), OrderBy (Id, Order, Value, UpdatedDate)
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderby"></param>
        /// <param name="ascending"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGet(Expression<Func<Event, bool>> filter, string orderby, bool ascending, string includeProperties, Action<IEnumerable<Event>, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                var serializer = new ExpressionSerializer(new BinarySerializer());
                var bytes = serializer.SerializeBinary(filter);
                var predicateDeserialized = serializer.DeserializeBinary(bytes);
                string filterString = predicateDeserialized.ToString();
                string key = ("causality_Event_tryget_" + filterString + "_" + orderby + "_" + ascending.ToString()).Replace(" ", "").ToLower() + "_" + includeProperties;
                List<Event> data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<List<Event>>(result.Value);
                        source = "indexedDB";
                    }
                    else if (await _onlineState.IsOnline())
                    {
                        getFromServer = true;
                    }
                    else
                    {
                        throw new Exception("No connection");
                    }
                }
                else
                {
                    getFromServer = true;
                }

                if (getFromServer)
                {
                    EventRequestGet req = new() { Filter = filterString, OrderBy = orderby, Ascending = ascending, IncludeProperties = includeProperties };
                    EventResponseGet ret = await _eventService.GetAsync(req);
                    if (ret.Success)
                    {
                        data = ret.Events.ToList();
                        source = ret.Status;
                        if (state.AppState.UseIndexedDB)
                        {
                            await _indexedDBManager.AddRecord(new StoreRecord<Blob> { Storename = "Blobs", Data = new Blob() { Key = key, Value = JsonConvert.SerializeObject(data) } });
                        }
                    }
                    else
                    {
                        throw new Exception("No connection");
                    }
                }

                onSuccess(data, RequestCodes.TWO_ZERO_ZERO + ", recived " + data.Count.ToString() + " record from " + source);

            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        /// <summary>
        /// TryGetById, Includes (Class, Cause, Effect, Exclude, Meta)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGetById(int id, string includeProperties, Action<Event, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string key = ("causality_Event_trygetbyid_" + id).Replace(" ", "").ToLower() + "_" + includeProperties;

                Event data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<Event>(result.Value);
                        source = "indexedDB";
                    }
                    else if (await _onlineState.IsOnline())
                    {
                        getFromServer = true;
                    }
                    else
                    {
                        throw new Exception("No connection");
                    }
                }
                else
                {
                    getFromServer = true;
                }

                if (getFromServer)
                {
                    EventRequestGetById req = new() { Id = id, IncludeProperties = includeProperties };
                    EventResponseGetById ret = await _eventService.GetByIdAsync(req);
                    if (ret.Success)
                    {
                        data = ret.Event;
                        source = ret.Status;
                        if (state.AppState.UseIndexedDB)
                        {
                            await _indexedDBManager.AddRecord(new StoreRecord<Blob> { Storename = "Blobs", Data = new Blob() { Key = key, Value = JsonConvert.SerializeObject(data) } });
                        }
                    }
                    else
                    {
                        throw new Exception("No connection");
                    }
                }

                onSuccess(data, RequestCodes.TWO_ZERO_ZERO + ", recived 1 record from " + source);

            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task TryInsert(Event Event, Action<Event, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    EventRequestInsert req = new() { Event = Event };
                    EventResponseInsert ret = await _eventService.InsertAsync(req);
                    if (ret.Success)
                    {
                        Event = ret.Event;
                        status = ret.Status;
                        if (state.AppState.UseIndexedDB)
                        {
                            await _indexedDBManager.OpenDb();
                            await _indexedDBManager.ClearStore("Blobs");
                        }
                    }
                    else
                    {
                        throw new Exception(ret.Status);
                    }
                }
                else
                {
                    throw new Exception(RequestCodes.FIVE_ZERO_FOUR);
                }

                onSuccess(Event, status);

            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task TryUpdate(Event Event, Action<Event, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    EventRequestUpdate req = new() { Event = Event };
                    EventResponseUpdate ret = await _eventService.UpdateAsync(req);
                    if (ret.Success)
                    {
                        Event = ret.Event;
                        status = ret.Status;
                        if (state.AppState.UseIndexedDB)
                        {
                            await _indexedDBManager.OpenDb();
                            await _indexedDBManager.ClearStore("Blobs");
                        }
                    }
                    else
                    {
                        throw new Exception(ret.Status);
                    }
                }
                else
                {
                    throw new Exception(RequestCodes.FIVE_ZERO_FOUR);
                }

                onSuccess(Event, status);

            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task WarmUp()
        {
            if (await _onlineState.IsOnline())
            {
                EventRequestGet req = new() { Filter = "e => e.Id > 0", OrderBy = "", Ascending = true, IncludeProperties = "Class,Cause,Effect,Exclude,Meta" };
                await _eventService.GetAsync(req);
            }
        }

    }

}
