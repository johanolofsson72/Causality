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
    public class StateService
    {
        readonly Causality.Shared.Models.StateService.StateServiceClient _stateService;
        readonly Causality.Shared.Models.MetaService.MetaServiceClient _metaService;
        readonly IndexedDBManager _indexedDBManager;
        readonly OnlineStateService _onlineState;

        public StateService(
            Causality.Shared.Models.StateService.StateServiceClient stateService,
            IndexedDBManager indexedDBManager,
            OnlineStateService onlineState, 
            Causality.Shared.Models.MetaService.MetaServiceClient metaService)
        {
            _stateService = stateService;
            _indexedDBManager = indexedDBManager;
            _onlineState = onlineState;
            _metaService = metaService;
        }

        public async Task TryDelete(int id, Action<string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                if (await _onlineState.IsOnline())
                {
                    StateRequestDelete req = new() { Id = id };
                    StateResponseDelete ret = await _stateService.DeleteAsync(req);
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
        /// TryGet, Includes (Meta), OrderBy (Id, EventId, CauseId, ClassId, UserId, Value, UpdatedDate)
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderby"></param>
        /// <param name="ascending"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGet(Expression<Func<State, bool>> filter, string orderby, bool ascending, string includeProperties, Action<IEnumerable<State>, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                var serializer = new ExpressionSerializer(new BinarySerializer());
                var bytes = serializer.SerializeBinary(filter);
                var predicateDeserialized = serializer.DeserializeBinary(bytes);
                string filterString = predicateDeserialized.ToString();
                string key = ("causality_state_tryget_" + filterString + "_" + orderby + "_" + ascending.ToString()).Replace(" ", "").ToLower();
                List<State> data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<List<State>>(result.Value);
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
                    StateRequestGet req = new() { Filter = filterString, OrderBy = orderby, Ascending = ascending };
                    StateResponseGet ret = await _stateService.GetAsync(req);
                    if (ret.Success)
                    {
                        foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            foreach (var item in ret.State)
                            {
                                if (includeProperty.ToLower().Equals("meta"))
                                {
                                    MetaRequestGet _req = new() { Filter = "e => e.Key LIKE '%StateId=" + item.Id + "%'", OrderBy = "Id", Ascending = true };
                                    MetaResponseGet _ret = await _metaService.GetAsync(_req);
                                    //item.Metas.Add(_ret.Metas);
                                    foreach (var m in _ret.Metas)
                                    {
                                        item.Meta.Add(new MetaCollection() { Meta = m });
                                    }
                                }
                            }
                        }
                        data = ret.State.ToList();
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
        /// TryGetById, Includes (Meta)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGetById(int id, string includeProperties, Action<State, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string key = ("causality_State_trygetbyid_" + id).Replace(" ", "").ToLower();

                State data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<State>(result.Value);
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
                    StateRequestGetById req = new() { Id = id };
                    StateResponseGetById ret = await _stateService.GetByIdAsync(req);
                    if (ret.Success)
                    {
                        foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (includeProperty.ToLower().Equals("meta"))
                            {
                                MetaRequestGet _req = new() { Filter = "e => e.Key LIKE '%StateId=" + ret.State.Id + "%'", OrderBy = "Id", Ascending = true };
                                MetaResponseGet _ret = await _metaService.GetAsync(_req);
                                //ret.State.Metas.Add(_ret.Metas);
                                foreach (var m in _ret.Metas)
                                {
                                    ret.State.Meta.Add(new MetaCollection() { Meta = m });
                                }
                            }
                        }
                        data = ret.State;
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

        public async Task TryInsert(State State, Action<State, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    StateRequestInsert req = new() { State = State };
                    StateResponseInsert ret = await _stateService.InsertAsync(req);
                    if (ret.Success)
                    {
                        State = ret.State;
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

                onSuccess(State, status);

            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task TryUpdate(State State, Action<State, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    StateRequestUpdate req = new() { State = State };
                    StateResponseUpdate ret = await _stateService.UpdateAsync(req);
                    if (ret.Success)
                    {
                        State = ret.State;
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

                onSuccess(State, status);

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
                StateRequestGet req = new() { Filter = "c => c.Id > 0", OrderBy = "", Ascending = true, IncludeProperties = "Meta" };
                await _stateService.GetAsync(req);
            }
        }

    }

}
