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
    public class ProcessService
    {
        readonly Causality.Shared.Models.ProcessService.ProcessServiceClient _processService;
        readonly IndexedDBManager _indexedDBManager;
        readonly OnlineStateService _onlineState;

        public ProcessService(IndexedDBManager indexedDBManager,
            OnlineStateService onlineState, 
            Causality.Shared.Models.ProcessService.ProcessServiceClient processService)
        {
            _indexedDBManager = indexedDBManager;
            _onlineState = onlineState;
            _processService = processService;
        }

        public async Task TryDelete(int id, Action<string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                if (await _onlineState.IsOnline())
                {
                    ProcessRequestDelete req = new() { Id = id };
                    ProcessResponseDelete ret = await _processService.DeleteAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
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
        /// TryGet, Includes (Metas), OrderBy (Id, EventId, Order, Value, UpdatedDate)
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderby"></param>
        /// <param name="ascending"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGet(Expression<Func<Process, bool>> filter, string orderby, bool ascending, string includeProperties, Action<IEnumerable<Process>, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                var serializer = new ExpressionSerializer(new BinarySerializer());
                var bytes = serializer.SerializeBinary(filter);
                var predicateDeserialized = serializer.DeserializeBinary(bytes);
                string filterString = predicateDeserialized.ToString();
                string key = ("causality_process_tryget_" + filterString + "_" + orderby + "_" + ascending.ToString()).Replace(" ", "").ToLower();
                List<Process> data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<List<Process>>(result.Value);
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
                    ProcessRequestGet req = new() { Filter = filterString, OrderBy = orderby, Ascending = ascending, IncludeProperties = includeProperties };
                    ProcessResponseGet ret = await _processService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        data = ret.Process.ToList();
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

                if(onSuccess is not null) onSuccess(data, RequestCodes.TWO_ZERO_ZERO + ", recived " + data.Count.ToString() + " record from " + source);

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
        /// TryGetById, Includes (Metas)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGetById(int id, string includeProperties, Action<Process, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string key = ("causality_Process_trygetbyid_" + id).Replace(" ", "").ToLower();

                Process data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<Process>(result.Value);
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
                    ProcessRequestGetById req = new() { Id = id, IncludeProperties = includeProperties };
                    ProcessResponseGetById ret = await _processService.GetByIdAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        data = ret.Process;
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

        public async Task TryInsert(Process Process, Action<Process, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    ProcessRequestInsert req = new() { Process = Process };
                    ProcessResponseInsert ret = await _processService.InsertAsync(req, deadline: DateTime.UtcNow.AddSeconds(50));
                    if (ret.Success)
                    {
                        Process = ret.Process;
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

                if(onSuccess is not null) onSuccess(Process, status);

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

        public async Task TryUpdate(Process Process, Action<Process, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    ProcessRequestUpdate req = new() { Process = Process };
                    ProcessResponseUpdate ret = await _processService.UpdateAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        Process = ret.Process;
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

                if(onSuccess is not null) onSuccess(Process, status);

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
                ProcessRequestGet req = new() { Filter = "c => c.Id > 0", OrderBy = "", Ascending = true, IncludeProperties = "Metas" };
                await _processService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
            }
        }

    }

}
