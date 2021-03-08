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
using Grpc.Core;

/// <summary>
/// Can be copied when adding new service
/// Mark the prefix before "xxxxService" and replace and you are good to go
/// </summary>
namespace Causality.Client.Services
{
    public class ClassService
    { 
        Causality.Shared.Models.ClassService.ClassServiceClient _classService;
        IndexedDBManager _indexedDBManager;
        OnlineStateService _onlineState;

        public ClassService(Causality.Shared.Models.ClassService.ClassServiceClient classService,
            IndexedDBManager indexedDBManager,
            OnlineStateService onlineState)
        {
            _classService = classService;
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
                    ClassResponseDelete ret = await _classService.DeleteAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
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

                if(onSuccess is not null) onSuccess(RequestCodes.TWO_ZERO_ZERO);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        /// <summary>
        /// TryGet, Includes (Causes, Effects, Metas), OrderBy (Id, EventId, Order, Value, UpdatedDate)
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
                var serializer = new ExpressionSerializer(new BinarySerializer());
                var bytes = serializer.SerializeBinary(filter);
                var predicateDeserialized = serializer.DeserializeBinary(bytes);
                string filterString = predicateDeserialized.ToString();
                string key = ("causality_Class_tryget_" + filterString + "_" + orderby + "_" + ascending.ToString()).Replace(" ", "").ToLower() + "_" + includeProperties;
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
                    ClassRequestGet req = new() { Filter = filterString, OrderBy = orderby, Ascending = ascending, IncludeProperties = includeProperties };
                    ClassResponseGet ret = await _classService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
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

                if(onSuccess is not null) if(onSuccess is not null) onSuccess(data, RequestCodes.TWO_ZERO_ZERO + ", recived " + data.Count.ToString() + " record from " + source);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        /// <summary>
        /// TryGetById, Includes (Causes, Effects, Metas)
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
                string key = ("causality_Class_trygetbyid_" + id).Replace(" ", "").ToLower() + "_" + includeProperties;

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
                    ClassRequestGetById req = new() { Id = id, IncludeProperties = includeProperties };
                    ClassResponseGetById ret = await _classService.GetByIdAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
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

                if(onSuccess is not null) onSuccess(data, RequestCodes.TWO_ZERO_ZERO + ", recived 1 record from " + source);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
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
                    ClassResponseInsert ret = await _classService.InsertAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
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

                if(onSuccess is not null) onSuccess(Class, status);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
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
                    ClassResponseUpdate ret = await _classService.UpdateAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
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

                if(onSuccess is not null) onSuccess(Class, status);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task WarmUp()
        {
            if (await _onlineState.IsOnline())
            {
                ClassRequestGet req = new() { Filter = "c => c.Id > 0", OrderBy = "", Ascending = true, IncludeProperties = "Causes,Effects,Metas" };
                await _classService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
            }
        }

    }

}
