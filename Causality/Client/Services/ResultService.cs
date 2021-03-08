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
    public class ResultService
    {
        readonly Causality.Shared.Models.ResultService.ResultServiceClient _resultService;
        readonly IndexedDBManager _indexedDBManager;
        readonly OnlineStateService _onlineState;

        public ResultService(
            Causality.Shared.Models.ResultService.ResultServiceClient resultService,
            IndexedDBManager indexedDBManager,
            OnlineStateService onlineState)
        {
            _resultService = resultService;
            _indexedDBManager = indexedDBManager;
            _onlineState = onlineState;
        }

        public async Task TryDelete(int id, Func<string, Task> onSuccess, Func<Exception, string, Task> onFail, CascadingAppStateProvider state)
        {
            try
            {
                if (await _onlineState.IsOnline())
                {
                    ResultRequestDelete req = new() { Id = id };
                    ResultResponseDelete ret = await _resultService.DeleteAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
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

                if(onSuccess is not null) await onSuccess(RequestCodes.TWO_ZERO_ZERO);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        /// <summary>
        /// TryGet, Includes (Metas), OrderBy (Id, EventId, ProcessId, CauseId, ClassId, UserId, Value, UpdatedDate)
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderby"></param>
        /// <param name="ascending"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGet(Expression<Func<Result, bool>> filter, string orderby, bool ascending, string includeProperties, Func<IEnumerable<Result>, string, Task> onSuccess, Func<Exception, string, Task> onFail, CascadingAppStateProvider state)
        {
            try
            {
                var serializer = new ExpressionSerializer(new BinarySerializer());
                var bytes = serializer.SerializeBinary(filter);
                var predicateDeserialized = serializer.DeserializeBinary(bytes);
                string filterString = predicateDeserialized.ToString();
                string key = ("causality_result_tryget_" + filterString + "_" + orderby + "_" + ascending.ToString()).Replace(" ", "").ToLower();
                List<Result> data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<List<Result>>(result.Value);
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
                    ResultRequestGet req = new() { Filter = filterString, OrderBy = orderby, Ascending = ascending };
                    ResultResponseGet ret = await _resultService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        data = ret.Result.ToList();
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

                if(onSuccess is not null) await onSuccess(data, RequestCodes.TWO_ZERO_ZERO + ", recived " + data.Count.ToString() + " record from " + source);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        /// <summary>
        /// TryGetById, Includes (Metas)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGetById(int id, string includeProperties, Func<Result, string, Task> onSuccess, Func<Exception, string, Task> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string key = ("causality_result_trygetbyid_" + id).Replace(" ", "").ToLower();

                Result data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<Result>(result.Value);
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
                    ResultRequestGetById req = new() { Id = id };
                    ResultResponseGetById ret = await _resultService.GetByIdAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        data = ret.Result;
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

                if(onSuccess is not null) await onSuccess(data, RequestCodes.TWO_ZERO_ZERO + ", recived 1 record from " + source);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task TryInsert(Result Result, Func<Result, string, Task> onSuccess, Func<Exception, string, Task> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    ResultRequestInsert req = new() { Result = Result };
                    ResultResponseInsert ret = await _resultService.InsertAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        Result = ret.Result;
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

                if(onSuccess is not null) await onSuccess(Result, status);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task TryUpdate(Result Result, Func<Result, string, Task> onSuccess, Func<Exception, string, Task> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    ResultRequestUpdate req = new() { Result = Result };
                    ResultResponseUpdate ret = await _resultService.UpdateAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        Result = ret.Result;
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

                if(onSuccess is not null) await onSuccess(Result, status);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                if(onFail is not null) await onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task WarmUp()
        {
            if (await _onlineState.IsOnline())
            {
                ResultRequestGet req = new() { Filter = "c => c.Id > 0", OrderBy = "", Ascending = true, IncludeProperties = "Metas" };
                await _resultService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
            }
        }

    }

}
