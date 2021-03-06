﻿using System;
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
    public class CauseService
    {
        Causality.Shared.Models.EffectService.EffectServiceClient _effectService;
        Causality.Shared.Models.CauseService.CauseServiceClient _causeService;
        Causality.Shared.Models.ExcludeService.ExcludeServiceClient _excludeService;
        IndexedDBManager _indexedDBManager;
        OnlineStateService _onlineState;

        public CauseService(Causality.Shared.Models.EffectService.EffectServiceClient effectService,
            Causality.Shared.Models.CauseService.CauseServiceClient causeService,
            Causality.Shared.Models.ExcludeService.ExcludeServiceClient excludeService,
            IndexedDBManager indexedDBManager,
            OnlineStateService onlineState)
        {
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
                    CauseRequestDelete req = new() { Id = id };
                    CauseResponseDelete ret = await _causeService.DeleteAsync(req);
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
        /// TryGet, Includes (Effect, Exclude), OrderBy (Id, EventId, ClassId, Order, Value, UpdatedDate)
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderby"></param>
        /// <param name="ascending"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGet(Expression<Func<Cause, bool>> filter, string orderby, bool ascending, string includeProperties, Action<IEnumerable<Cause>, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                var serializer = new ExpressionSerializer(new BinarySerializer());
                var bytes = serializer.SerializeBinary(filter);
                var predicateDeserialized = serializer.DeserializeBinary(bytes);
                string filterString = predicateDeserialized.ToString();
                string key = ("causality_cause_tryget_" + filterString + "_" + orderby + "_" + ascending.ToString()).Replace(" ", "").ToLower();
                List<Cause> data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<List<Cause>>(result.Value);
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
                    CauseRequestGet req = new() { Filter = filterString, OrderBy = orderby, Ascending = ascending };
                    CauseResponseGet ret = await _causeService.GetAsync(req);
                    if (ret.Success)
                    {
                        foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            foreach (var item in ret.Causes)
                            {
                                if (includeProperty.ToLower().Equals("effect"))
                                {
                                    EffectRequestGet _req = new() { Filter = "e => e.CauseId = " + item.Id, OrderBy = "Id", Ascending = true };
                                    EffectResponseGet _ret = await _effectService.GetAsync(_req);
                                    item.Effects.Add(_ret.Effects);
                                }
                                if (includeProperty.ToLower().Equals("exclude"))
                                {
                                    ExcludeRequestGet _req = new() { Filter = "e => e.CauseId = " + item.Id, OrderBy = "Id", Ascending = true };
                                    ExcludeResponseGet _ret = await _excludeService.GetAsync(_req);
                                    item.Excludes.Add(_ret.Excludes);
                                }
                            }
                        }
                        data = ret.Causes.ToList();
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
        /// TryGetById, Includes (Effect, Exclude)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGetById(int id, string includeProperties, Action<Cause, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string key = ("causality_Cause_trygetbyid_" + id).Replace(" ", "").ToLower();

                Cause data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<Cause>(result.Value);
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
                    CauseRequestGetById req = new() { Id = id };
                    CauseResponseGetById ret = await _causeService.GetByIdAsync(req);
                    if (ret.Success)
                    {
                        foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (includeProperty.ToLower().Equals("effect"))
                            {
                                EffectRequestGet _req = new() { Filter = "e => e.CauseId = " + ret.Cause.Id, OrderBy = "Id", Ascending = true };
                                EffectResponseGet _ret = await _effectService.GetAsync(_req);
                                ret.Cause.Effects.Add(_ret.Effects);
                            }
                            if (includeProperty.ToLower().Equals("exclude"))
                            {
                                ExcludeRequestGet _req = new() { Filter = "e => e.CauseId = " + ret.Cause.Id, OrderBy = "Id", Ascending = true };
                                ExcludeResponseGet _ret = await _excludeService.GetAsync(_req);
                                ret.Cause.Excludes.Add(_ret.Excludes);
                            }
                        }
                        data = ret.Cause;
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

        public async Task TryInsert(Cause Cause, Action<Cause, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    CauseRequestInsert req = new() { Cause = Cause };
                    CauseResponseInsert ret = await _causeService.InsertAsync(req);
                    if (ret.Success)
                    {
                        Cause = ret.Cause;
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

                onSuccess(Cause, status);

            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task TryUpdate(Cause Cause, Action<Cause, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    CauseRequestUpdate req = new() { Cause = Cause };
                    CauseResponseUpdate ret = await _causeService.UpdateAsync(req);
                    if (ret.Success)
                    {
                        Cause = ret.Cause;
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

                onSuccess(Cause, status);

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
                CauseRequestGet req = new() { Filter = "c => c.Id > 0", OrderBy = "", Ascending = true, IncludeProperties = "Effect,Exclude" };
                await _causeService.GetAsync(req);
            }
        }

    }

}
