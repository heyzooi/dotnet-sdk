﻿// Copyright (c) 2019, Kinvey, Inc. All rights reserved.
//
// This software is licensed to you under the Kinvey terms of service located at
// http://www.kinvey.com/terms-of-use. By downloading, accessing and/or using this
// software, you hereby accept such terms of service  (and any agreement referenced
// therein) and agree that you have read, understand and agree to be bound by such
// terms of service and are of legal age to agree to such terms with Kinvey.
//
// This software contains valuable confidential and proprietary information of
// KINVEY, INC and is subject to applicable licensing agreements.
// Unauthorized reproduction, transmission or distribution of this file and its
// contents is a violation of applicable laws.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Kinvey
{
    /// <summary>
    /// Base class for creating requests to read data.
    /// </summary>
    /// <typeparam name="T">The type of the network request.</typeparam>
    /// <typeparam name="U">The type of the network response.</typeparam>
	public abstract class ReadRequest <T, U> : Request <T, U>
	{
        /// <summary>
		/// Gets the interface for operating with data store cache.
		/// </summary>
		/// <value>The instance implementing <see cref="ICache{T}" /> interface.</value>
		public ICache<T> Cache { get; }

        /// <summary>
		/// Gets collection name for the request.
		/// </summary>
		/// <value>String value with collection name.</value>
		public string Collection { get; }

        /// <summary>
        /// Gets read policy for the request.
        /// </summary>
        /// <value> <see cref="ReadPolicy" /> enum value containing read policy for the request.</value>
        public ReadPolicy Policy { get; }

        /// <summary>
        /// Gets query for the request.
        /// </summary>
        /// <value>  <see cref="IQueryable{Object}" /> value containing query for the request.</value>
		protected IQueryable<object> Query { get; }

        /// <summary>
        /// Indicates whether delta set fetching is enabled on this request, defaulted to false.
        /// </summary>
        /// <value><c>true</c> if delta set fetching enabled; otherwise, <c>false</c>.</value>
        protected bool DeltaSetFetchingEnabled { get; }

        /// <summary>
        /// Gets entity ids for the request.
        /// </summary>
        /// <value>The list of entity ids</value>
		protected List<string> EntityIDs { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadRequest{T,U}"/> class.
        /// </summary>
        /// <param name="client">Client that the user is logged in.</param>
        /// <param name="collection">Collection name.</param>
        /// <param name="cache">Cache.</param>
        /// <param name="query">Query.</param>
        /// <param name="policy">Read policy.</param>
        /// <param name="deltaSetFetchingEnabled">If set to <c>true</c> delta set fetching enabled.</param>
        public ReadRequest(AbstractClient client, string collection, ICache<T> cache, IQueryable<object> query, ReadPolicy policy, bool deltaSetFetchingEnabled)
	: base(client)
		{
			this.Cache = cache;
			this.Collection = collection;
			this.Query = query;
			this.Policy = policy;
			this.DeltaSetFetchingEnabled = deltaSetFetchingEnabled;
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadRequest{T,U}"/> class.
        /// </summary>
        /// <param name="client">Client that the user is logged in.</param>
        /// <param name="collection">Collection name.</param>
        /// <param name="cache">Cache.</param>
        /// <param name="query">Query.</param>
        /// <param name="policy">Read policy.</param>
        /// <param name="deltaSetFetchingEnabled">If set to <c>true</c> delta set fetching enabled.</param>
        /// <param name="entityIds">The list of entity ids.</param>
		public ReadRequest (AbstractClient client, string collection, ICache<T> cache, IQueryable<object> query, ReadPolicy policy, bool deltaSetFetchingEnabled, List<String> entityIds)
			: base(client)
		{
			this.Cache = cache;
			this.Collection = collection;
			this.Query = query;
			this.Policy = policy;
			this.DeltaSetFetchingEnabled = deltaSetFetchingEnabled;
			this.EntityIDs = entityIds;
		}

		/// <summary>
		/// Builds the mongo-style query string to be run against the backend.
		/// </summary>
		/// <returns>The mongo-style query string.</returns>
		protected string BuildMongoQuery()
		{
            return KinveyMongoQueryBuilder.GetQueryForFindOperation<T>(Query);
        }

        /// <summary>
		/// Operating with delta set data.
		/// </summary>
        /// <param name="cacheItems">Cache items.</param>
        /// <param name="networkItems">Network items.</param>
        /// <param name="mongoQuery">Mongo query.</param>
		/// <returns>The async task with the list of entities.</returns>
		protected async Task<List<T>> RetrieveDeltaSet(List<T> cacheItems, List<DeltaSetFetchInfo> networkItems, string mongoQuery)
		{
			List<T> listDeltaSetResults = new List<T>();

			#region DSF Step 2: Pull all entity IDs and LMTs of a collection in local storage

			Dictionary<string, string> dictCachedEntities = new Dictionary<string, string>();

			foreach (var cacheItem in cacheItems)
			{
				var item = cacheItem as IPersistable;
				if (item.Kmd?.lastModifiedTime != null) {  //if lmt doesn't exist for cache entity, avoid crashing
					dictCachedEntities.Add(item.ID, item.Kmd.lastModifiedTime);
				}
			}

			List<string> listCachedEntitiesToRemove = new List<string>(dictCachedEntities.Keys);

			#endregion

			#region DSF Step 3: Compare backend and local entities to see what has been created, deleted and updated since the last fetch

			List<string> listIDsToFetch = new List<string>();

			foreach (var networkEntity in networkItems)
			{
				string ID = networkEntity.ID;
				string LMT = networkEntity.KMD.lastModifiedTime;

				if (!dictCachedEntities.ContainsKey(ID))
				{
					// Case where a new item exists in the backend, but not in the local cache
					listIDsToFetch.Add(ID);
				}
				else if (HelperMethods.IsDateMoreRecent(LMT, dictCachedEntities[ID]))
				{
					// Case where the backend has a more up-to-date version of the entity than the local cache
					listIDsToFetch.Add(ID);
				}

				// Case where the backend has deleted an item that has not been removed from local storage.
				//
				// To begin with, this list has all the IDs currently present in local storage.  If an ID
				// has been found in the set of backend IDs, we will remove it from this list.  What will
				// remain in this list are all the IDs that are currently in local storage that
				// are not present in the backend, and therefore have to be deleted from local storage.
				listCachedEntitiesToRemove.Remove(ID);

				// NO-OPS: Should never hit these cases, because a Push() has to happen prior to a pull
				// 		Case where a new item exists in the local cache, but not in the backend
				// 		Case where the local cache has a more up-to-date version of the entity than the backend
				// 		Case where the local cache has deleted an item that has not been removed from the backend
			}

			#endregion

			#region DSF Step 4: Remove items from local storage that are no longer in the backend

			Cache.DeleteByIDs(listCachedEntitiesToRemove);

			#endregion

			#region DSF Step 5: Fetch selected IDs from backend to update local storage

			// Then, with this set of IDs from the previous step, make a query to the
			// backend, to get full records for each ID that has changed since last fetch.
			int numIDs = listIDsToFetch.Count;

			if (numIDs == networkItems.Count) {
				//Special case where delta set is the same size as the network result.
				//This will occur either when all entities are new/updated, or in error cases such as missing lmts
				return await RetrieveNetworkResults(mongoQuery).ConfigureAwait(false);
			}

			int start = 0;
			int batchSize = 200;

			while (start < numIDs)
			{
				int count = Math.Min((numIDs - start), batchSize);
				string queryIDs = BuildIDsQuery(listIDsToFetch.GetRange(start, count));
				List<T> listBatchResults = await Client.NetworkFactory.buildGetRequest<T>(Collection, queryIDs).ExecuteAsync().ConfigureAwait(false);

				start += listBatchResults.Count();
				listDeltaSetResults.AddRange(listBatchResults);
			}

			#endregion

			return listDeltaSetResults;
		}

        /// <summary>
		/// Perfoms finding in a local storage.
		/// </summary>
        /// <param name="localDelegate">[optional] Delegate for returning results.</param>
		/// <returns>The list of entities.</returns>
		protected List<T> PerformLocalFind(KinveyDelegate<List<T>> localDelegate = null)
		{
			List<T> cacheHits = default(List<T>);

			try
			{
				if (Query != null)
				{
					var query = Query;
					cacheHits = Cache.FindByQuery(query.Expression);
				}
				else if (EntityIDs?.Count > 0)
				{
					cacheHits = Cache.FindByIDs(EntityIDs);
				}
				else
				{
					cacheHits = Cache.FindAll();
				}

				localDelegate?.onSuccess(cacheHits);
			}
			catch (Exception e)
			{
				if (localDelegate != null)
				{
					localDelegate.onError(e);
				}
				else
				{
					throw;
				}
			}

			return cacheHits;
		}

        /// <summary>
		/// Perfoms finding in backend.
		/// </summary>
		/// <returns>The async task with the request results.</returns>
		protected async Task<NetworkReadResponse<T>> PerformNetworkFind()
		{
			try
			{
				string mongoQuery = this.BuildMongoQuery();

                bool isQueryModifierPresent = !string.IsNullOrEmpty(mongoQuery) ?
                                                     mongoQuery.Contains(Constants.STR_QUERY_MODIFIER_SKIP) ||
                                                     mongoQuery.Contains(Constants.STR_QUERY_MODIFIER_LIMIT) :
                                                     false;

                if (DeltaSetFetchingEnabled && !isQueryModifierPresent)
				{
                    QueryCacheItem queryCacheItem = Client.CacheManager.GetQueryCacheItem(Collection, mongoQuery, null);

                    if (!Cache.IsCacheEmpty())
                    {
                        if (queryCacheItem != null && !string.IsNullOrEmpty(queryCacheItem.lastRequest))
                        {
                            // Able to perform server-side delta set fetch
                            NetworkRequest<DeltaSetResponse<T>> request = Client.NetworkFactory.BuildDeltaSetRequest<DeltaSetResponse<T>>(queryCacheItem.collectionName, queryCacheItem.lastRequest, queryCacheItem.query);
                            DeltaSetResponse<T> results = null;
                            try
                            {
                               results  = await request.ExecuteAsync().ConfigureAwait(false);
                            }
                            catch (KinveyException ke)
                            {
                                // Regardless of the error, remove the QueryCacheItem if it exists
                                Client.CacheManager.DeleteQueryCacheItem(queryCacheItem);

                                switch (ke.StatusCode)
                                {
                                    case 400: // ResultSetSizeExceeded or ParameterValueOutOfRange
                                        if (ke.Error.Equals(Constants.STR_ERROR_BACKEND_RESULT_SET_SIZE_EXCEEDED))
                                        {
                                            // This means that there are greater than 10k items in the delta set.
                                            // Clear QueryCache table, perform a regular GET
                                            // and capture x-kinvey-request-start time
                                            return await PerformNetworkInitialDeltaGet(mongoQuery).ConfigureAwait(false);
                                        }
                                        else if (ke.Error.Equals(Constants.STR_ERROR_BACKEND_PARAMETER_VALUE_OUT_OF_RANGE))
                                        {
                                            // This means that the last sync time for delta set is too far back, or
                                            // the backend was enabled for delta set after the client was enabled
                                            // and already attempted a GET.

                                            // Perform regular GET and capture x-kinvey-request-start time
                                            return await PerformNetworkInitialDeltaGet(mongoQuery).ConfigureAwait(false);
                                        }
                                        break;

                                    case 403: // MissingConfiguration
                                        if (ke.Error.Equals(Constants.STR_ERROR_BACKEND_MISSING_CONFIGURATION))
                                        {
                                            // This means that server-side delta sync
                                            // is not enabled - should perform a regular
                                            // GET and capture x-kinvey-request-start time
                                            return await PerformNetworkInitialDeltaGet(mongoQuery).ConfigureAwait(false);
                                        }
                                        break;

                                    default:
                                        // This is not a delta sync specific error
                                        throw;
                                }
                            }

                            // With the _deltaset endpoint result from the server:

                            // 1 - Apply deleted set to local cache
                            List<string> listDeletedIDs = new List<string>();
                            foreach (var deletedItem in results.Deleted)
                            {
                                listDeletedIDs.Add(deletedItem.ID);
                            }
                            Cache.DeleteByIDs(listDeletedIDs);

                            // 2 - Apply changed set to local cache
                            Cache.RefreshCache(results.Changed);

                            // 3 - Update the last request time for this combination
                            // of collection:query
                            queryCacheItem.lastRequest = request.RequestStartTime;
                            Client.CacheManager.SetQueryCacheItem(queryCacheItem);

                            // 4 - Return network results
                            return new NetworkReadResponse<T>(results.Changed, results.Changed.Count, true);
                        }
                        else
                        {
                            // Perform regular GET and capture x-kinvey-request-start time
                            return await PerformNetworkInitialDeltaGet(mongoQuery).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // Perform regular GET and capture x-kinvey-request-start time
                        return await PerformNetworkInitialDeltaGet(mongoQuery, queryCacheItem).ConfigureAwait(false);
                    }
				}

                return await PerformNetworkGet(mongoQuery).ConfigureAwait(false);
			}
			catch (KinveyException)
			{
				throw;
			}
            catch (Exception e)
			{
				throw new KinveyException(EnumErrorCategory.ERROR_DATASTORE_NETWORK,
										  EnumErrorCode.ERROR_GENERAL,
										  "Error in FindAsync() for network results.",
										  e);
			}
		}

        /// <summary>
		/// Retrieves entities from backend.
		/// </summary>
        /// <param name="mongoQuery">Mongo query.</param>
		/// <returns>The async task with the list of entities.</returns>
        protected async Task<List<T>> RetrieveNetworkResults(string mongoQuery)
		{
			List<T> networkResults = default(List<T>);

			if (Query != null)
			{
				networkResults = await Client.NetworkFactory.buildGetRequest<T>(Collection, mongoQuery).ExecuteAsync().ConfigureAwait(false);
			}
			else if (EntityIDs?.Count > 0)
			{
				networkResults = new List<T>();
				foreach (string entityID in EntityIDs)
				{
					T item = await Client.NetworkFactory.buildGetByIDRequest<T>(Collection, entityID).ExecuteAsync().ConfigureAwait(false);
					networkResults.Add(item);
				}
			}
			else
			{
				networkResults = await Client.NetworkFactory.buildGetRequest<T>(Collection).ExecuteAsync().ConfigureAwait(false);
			}

			return networkResults;
		}

		private string BuildIDsQuery(List<string> listIDs)
		{
			System.Text.StringBuilder query = new System.Text.StringBuilder();

			query.Append("{\"_id\": { \"$in\": [");

			bool isNotFirstID = false;
			foreach (var ID in listIDs)
			{
				if (isNotFirstID)
				{
					query.Append(",");
				}

				query.Append("\"");
				query.Append(ID);
				query.Append("\"");

				isNotFirstID = true;
			}

			query.Append("] } }");

			// TODO need to add back in any modifiers from original query

			return query.ToString();
		}

        private async Task<NetworkReadResponse<T>> PerformNetworkGet(string mongoQuery)
        {
            var results = await RetrieveNetworkResults(mongoQuery).ConfigureAwait(false);
            Cache.Clear(Query?.Expression);
            Cache.RefreshCache(results);
            return new NetworkReadResponse<T>(results, results.Count, false);
        }

        private async Task<NetworkReadResponse<T>> PerformNetworkInitialDeltaGet(string mongoQuery, QueryCacheItem queryCacheItem = null)
        {
            var getResult = Client.NetworkFactory.buildGetRequest<T>(Collection, mongoQuery);
            List<T> results = await getResult.ExecuteAsync().ConfigureAwait(false);
            Cache.Clear(Query?.Expression);
            Cache.RefreshCache(results);
            string lastRequestTime = getResult.RequestStartTime;

            if (queryCacheItem != null && !string.IsNullOrEmpty(queryCacheItem.lastRequest))
            {
                queryCacheItem.lastRequest = getResult.RequestStartTime;
            }
            else
            {
                queryCacheItem = new QueryCacheItem(Collection, mongoQuery, lastRequestTime);
            }

            Client.CacheManager.SetQueryCacheItem(queryCacheItem);

            return new NetworkReadResponse<T>(results, results.Count, false);
        }

        /// <summary>
        /// This class represents the response of a network read request.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        protected class NetworkReadResponse<T>
		{
            /// <summary>
            /// Result set from the network request.
            /// </summary>
            /// <value>The list of entities.</value>
            public List<T> ResultSet;

            /// <summary>
            /// Total count of entities.
            /// </summary>
            /// <value>The value with total count.</value>
            public int TotalCount;

            /// <summary>
            /// Indicates whether delta set fetching is enabled, defaulted to false.
            /// </summary>
            /// <value><c>true</c> if delta set fetching enabled; otherwise, <c>false</c>.</value>
            public bool IsDeltaFetched;

            /// <summary>
            /// Initializes a new instance of the <see cref="NetworkReadResponse{T}"/> class.
            /// </summary>
            /// <param name="result">List of entities.</param>
            /// <param name="count">Total count.</param>
            /// <param name="isDelta"><c>true</c> if delta set fetching enabled; otherwise, <c>false</c>.</param>
            public NetworkReadResponse(List<T> result, int count, bool isDelta)
			{
				this.ResultSet = result;
				this.TotalCount = count;
				this.IsDeltaFetched = isDelta;
			}
		}
	}
}
