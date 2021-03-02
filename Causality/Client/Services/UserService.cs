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
    public class UserService
    {
        Causality.Shared.Models.UserService.UserServiceClient _userService;
        IndexedDBManager _indexedDBManager;
        OnlineStateService _onlineState;

        public UserService(Causality.Shared.Models.UserService.UserServiceClient userService,
            IndexedDBManager indexedDBManager,
            OnlineStateService onlineState)
        {
            _userService = userService;
            _indexedDBManager = indexedDBManager;
            _onlineState = onlineState;
        }

        public async Task TryDelete(int id, Action<string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                if (state.AppState.UseIndexedDB)
                {
                    if (await _onlineState.IsOnline())
                    {
                        UserRequestDelete req = new() { Id = id };
                        UserResponseDelete ret = await _userService.DeleteAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));

                        if (!ret.Success)
                        {
                            onFail(new Exception(RequestCodes.FIVE_ZERO_THREE), ret.Status);
                            return;
                        }

                        await _indexedDBManager.OpenDb();
                        await _indexedDBManager.ClearStore("Blobs");

                        onSuccess(ret.Status);
                        return;
                    }
                    else
                    {
                        onFail(new Exception(RequestCodes.FIVE_ZERO_FOUR), " Could Not Delete, Please Try Again Later");
                        return;
                    }
                }
                else if (await _onlineState.IsOnline())
                {
                    UserRequestDelete req = new() { Id = id };
                    UserResponseDelete ret = await _userService.DeleteAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));

                    if (!ret.Success)
                    {
                        onFail(new Exception(RequestCodes.FIVE_ZERO_THREE), ret.Status);
                        return;
                    }

                    onSuccess(ret.Status);
                    return;
                }
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
        /// TryGet, Includes (Excludes, Metas), OrderBy (Id, UID, IP, Name, Email, UpdatedDate)
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderby"></param>
        /// <param name="ascending"></param>
        /// <param name="includeProperties"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGet(Expression<Func<User, bool>> filter, string orderby, bool ascending, string includeProperties, Action<IEnumerable<User>, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                var serializer = new ExpressionSerializer(new BinarySerializer());
                var bytes = serializer.SerializeBinary(filter);
                var predicateDeserialized = serializer.DeserializeBinary(bytes);
                string filterString = predicateDeserialized.ToString();
                string key = ("causality_User_tryget_" + filterString + "_" + orderby + "_" + ascending.ToString()).Replace(" ", "").ToLower();
                List<User> data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<List<User>>(result.Value);
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
                    UserRequestGet req = new() { Filter = filterString, OrderBy = orderby, Ascending = ascending, IncludeProperties = includeProperties };
                    UserResponseGet ret = await _userService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        data = ret.Users.ToList();
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
        /// TryGetById, Includes (Excludes, Metas)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onFail"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public async Task TryGetById(int id, string includeProperties, Action<User, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string key = ("causality_User_trygetbyid_" + id).Replace(" ", "").ToLower();

                User data = new();
                bool getFromServer = false;
                string source = "";

                if (state.AppState.UseIndexedDB)
                {
                    var result = await _indexedDBManager.GetRecordByIndex<string, Blob>(new StoreIndexQuery<string> { Storename = _indexedDBManager.Stores[0].Name, IndexName = "key", QueryValue = key });
                    if (result is not null)
                    {
                        data = JsonConvert.DeserializeObject<User>(result.Value);
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
                    UserRequestGetById req = new() { Id = id, IncludeProperties = includeProperties };
                    UserResponseGetById ret = await _userService.GetByIdAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        data = ret.User;
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

        public async Task TryInsert(User User, Action<User, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    UserRequestInsert req = new() { User = User };
                    UserResponseInsert ret = await _userService.InsertAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        User = ret.User;
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

                onSuccess(User, status);

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

        public async Task TryUpdate(User User, Action<User, string> onSuccess, Action<Exception, string> onFail, CascadingAppStateProvider state)
        {
            try
            {
                string status = "";
                if (await _onlineState.IsOnline())
                {
                    UserRequestUpdate req = new() { User = User };
                    UserResponseUpdate ret = await _userService.UpdateAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
                    if (ret.Success)
                    {
                        User = ret.User;
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

                onSuccess(User, status);

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
                UserRequestGet req = new() { Filter = "u => u.Id > 0", OrderBy = "", Ascending = true, IncludeProperties = "Metas,Excludes"};
                await _userService.GetAsync(req, deadline: DateTime.UtcNow.AddSeconds(5));
            }
        }

    }

}
