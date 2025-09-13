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
    public class ClassService
    { 
        Causality.Shared.Models.EffectService.EffectServiceClient _effectService;
        Causality.Shared.Models.CauseService.CauseServiceClient _causeService;
        Causality.Shared.Models.ClassService.ClassServiceClient _classService;
        IndexedDBManager _indexedDBManager;
        OnlineStateService _onlineState;

        public ClassService(Causality.Shared.Models.EffectService.EffectServiceClient effectService,
            Causality.Shared.Models.CauseService.CauseServiceClient causeService,
            Causality.Shared.Models.ClassService.ClassServiceClient classService,
            IndexedDBManager indexedDBManager,
            OnlineStateService onlineState)
        {
            _classService = classService;
            _effectService = effectService;
            _causeService = causeService;
            _indexedDBManager = indexedDBManager;
            _onlineState = onlineState;
        }

        public async Task TryDelete(int id, Action<string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                if (await _onlineState.IsOnline())
                {
                    ClassRequestDelete req = new() { Id = id };
                    ClassResponseDelete ret = await _classService.DeleteAsync(req);
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
        /// TryGet, Includes (Cause, Effect), OrderBy (Id, EventId, Order, Value, UpdatedDate)
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderby"></param>
        /// <param name="ascending"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGet(Expression<Func<Class, bool>> filter, string orderby, bool ascending, string includeProperties, Action<IEnumerable<Class>, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                var serializer = new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer());
                var bytes = serializer.SerializeBinary(filter);
                var predicateDeserialized = serializer.DeserializeBinary(bytes);
                string filterString = predicateDeserialized.ToString();
                string key = ("causality_Class_tryget_" + filterString + "_" + orderby + "_" + ascending.ToString()).Replace(" ", "").ToLower();
                List<Class> data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<List<Class>>(result.Value);
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
                    ClassRequestGet req = new() { Filter = filterString, OrderBy = orderby, Ascending = ascending };
                    ClassResponseGet ret = await _classService.GetAsync(req);
                    if (ret.Success)
                    {
                        foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            foreach (var item in ret.Classes)
                            {
                                if (includeProperty.ToLower().Equals("cause"))
                                {
                                    CauseRequestGet _req = new() { Filter = "c => c.ClassId = " + item.Id, OrderBy = "Id", Ascending = true };
                                    CauseResponseGet _ret = await _causeService.GetAsync(_req);
                                    item.Causes.Add(_ret.Causes);
                                }
                                if (includeProperty.ToLower().Equals("effect"))
                                {
                                    EffectRequestGet _req = new() { Filter = "e => e.ClassId = " + item.Id, OrderBy = "Id", Ascending = true };
                                    EffectResponseGet _ret = await _effectService.GetAsync(_req);
                                    item.Effects.Add(_ret.Effects);
                                }
                            }
                        }
                        data = ret.Classes.ToList();
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
        /// TryGetById, Includes (Cause, Effect)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGetById(int id, string includeProperties, Action<Class, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string key = ("causality_Class_trygetbyid_" + id).Replace(" ", "").ToLower();

                Class data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<Class>(result.Value);
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
                    ClassRequestGetById req = new() { Id = id };
                    ClassResponseGetById ret = await _classService.GetByIdAsync(req);
                    if (ret.Success)
                    {
                        foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (includeProperty.ToLower().Equals("cause"))
                            {
                                CauseRequestGet _req = new() { Filter = "c => c.ClassId = " + ret.Class.Id, OrderBy = "Id", Ascending = true };
                                CauseResponseGet _ret = await _causeService.GetAsync(_req);
                                ret.Class.Causes.Add(_ret.Causes);
                            }
                            if (includeProperty.ToLower().Equals("effect"))
                            {
                                EffectRequestGet _req = new() { Filter = "e => e.ClassId = " + ret.Class.Id, OrderBy = "Id", Ascending = true };
                                EffectResponseGet _ret = await _effectService.GetAsync(_req);
                                ret.Class.Effects.Add(_ret.Effects);
                            }
                        }
                        data = ret.Class;
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

        public async Task TryInsert(Class Class, Action<Class, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    ClassRequestInsert req = new() { Class = Class };
                    ClassResponseInsert ret = await _classService.InsertAsync(req);
                    if (ret.Success)
                    {
                        Class = ret.Class;
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

                onSuccess(Class, status);

            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task TryUpdate(Class Class, Action<Class, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    ClassRequestUpdate req = new() { Class = Class };
                    ClassResponseUpdate ret = await _classService.UpdateAsync(req);
                    if (ret.Success)
                    {
                        Class = ret.Class;
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

                onSuccess(Class, status);

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
                ClassRequestGet req = new() { Filter = "c => c.Id > 0", OrderBy = "", Ascending = true, IncludeProperties = "Cause,Effect" };
                await _classService.GetAsync(req);
            }
        }

    }

}
