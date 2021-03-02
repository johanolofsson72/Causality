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
using System.Linq.Expressions;
using Serialize.Linq.Serializers;
using System.Text;
using Grpc.Core;

/// <summary>
/// Can be copied when adding new service
/// Mark the prefix before "xxxxService" and replace and you are good to go
/// </summary>
namespace Causality.Client.Services
{
    public class EffectService
    {
        Causality.Shared.Models.EffectService.EffectServiceClient _effectService;
        IndexedDBManager _indexedDBManager;
        OnlineStateService _onlineState;

        public EffectService(Causality.Shared.Models.EffectService.EffectServiceClient effectService, IndexedDBManager indexedDBManager, OnlineStateService onlineState)
        {
            _effectService = effectService;
            _indexedDBManager = indexedDBManager;
            _onlineState = onlineState;
        }

        public async Task TryDelete(int id, Action<string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                if (await _onlineState.IsOnline())
                {
                    EffectRequestDelete req = new() { Id = id };
                    EffectResponseDelete ret = await _effectService.DeleteAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
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
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        /// <summary>
        /// TryGet, Includes (Metas), OrderBy (Id, EventId, ClassId, CauseId, UserId, Value, UpdatedDate)
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderby"></param>
        /// <param name="ascending"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGet(Expression<Func<Effect, bool>> filter, string orderby, bool ascending, string includeProperties, Action<IEnumerable<Effect>, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                var serializer = new ExpressionSerializer(new BinarySerializer());
                var bytes = serializer.SerializeBinary(filter);
                var predicateDeserialized = serializer.DeserializeBinary(bytes);
                string filterString = predicateDeserialized.ToString();
                string key = ("causality_effect_tryget_" + filterString + "_" + orderby + "_" + ascending.ToString()).Replace(" ", "").ToLower() + "_" + includeProperties;
                List<Effect> data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<List<Effect>>(result.Value);
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
                    EffectRequestGet req = new() { Filter = filterString, OrderBy = orderby, Ascending = ascending, IncludeProperties = includeProperties };
                    EffectResponseGet ret = await _effectService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        data = ret.Effects.ToList();
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
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        /// <summary>
        /// TryGetById, Includes (Metas)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGetById(int id, string includeProperties, Action<Effect, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string key = ("causality_Effect_trygetbyid_" + id).Replace(" ", "").ToLower() + "_" + includeProperties;

                Effect data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<Effect>(result.Value);
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
                    EffectRequestGetById req = new() { Id = id, IncludeProperties = includeProperties };
                    EffectResponseGetById ret = await _effectService.GetByIdAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        data = ret.Effect;
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
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task TryInsert(Effect Effect, Action<Effect, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    EffectRequestInsert req = new() { Effect = Effect };
                    EffectResponseInsert ret = await _effectService.InsertAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        Effect = ret.Effect;
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

                onSuccess(Effect, status);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
            catch (Exception e)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
            }
        }

        public async Task TryUpdate(Effect Effect, Action<Effect, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    EffectRequestUpdate req = new() { Effect = Effect };
                    EffectResponseUpdate ret = await _effectService.UpdateAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        Effect = ret.Effect;
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

                onSuccess(Effect, status);

            }
            catch (RpcException e) when (e.StatusCode == StatusCode.DeadlineExceeded)
            {
                onFail(e, RequestCodes.FIVE_ZERO_ZERO);
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
                EffectRequestGet req = new() { Filter = "e => e.Id > 0", OrderBy = "", Ascending = true, IncludeProperties = "Metas" };
                await _effectService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
            }
        }

    }

}
