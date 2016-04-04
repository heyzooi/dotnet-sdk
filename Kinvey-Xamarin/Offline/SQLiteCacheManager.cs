﻿using System;
using System.Threading.Tasks;
using SQLite.Net;
using SQLite.Net.Async;
using SQLite.Net.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;


namespace KinveyXamarin
{
	/// <summary>
	/// SQLite cache manager.
	/// </summary>
	public class SQLiteCacheManager : ICacheManager
	{

		//The version of the internal structure of the database.
		private int databaseSchemaVersion = 1;

		/// <summary>
		/// The db connection.
		/// </summary>
		private SQLiteAsyncConnection dbConnection;


		/// <summary>
		/// Gets or sets the platform.
		/// </summary>
		/// <value>The platform.</value>
		public ISQLitePlatform platform {get; set;}

		/// <summary>
		/// Gets or sets the database file path.
		/// </summary>
		/// <value>The dbpath.</value>
		public string dbpath{ get; set;}


		/// <summary>
		/// Initializes a new instance of the <see cref="KinveyXamarin.SQLiteCacheManager"/> class.
		/// </summary>
		public SQLiteCacheManager (ISQLitePlatform platform, string filePath){
			this.platform = platform;
			this.dbpath = Path.Combine (filePath, "kinveyOffline.sqlite");

			new Task( () =>kickOffUpgrade() ).Start();
			//			Task.Run (kickOffUpgrade ());
		}

		private async Task<int> kickOffUpgrade(){
			//get stored version number, if it's null set it to the current dbscheme version and save it it
			//call onupgrade with current version number and dbsv.
			//DatabaseHelper<JObject> handler = getDatabaseHelper<JObject> ();
			SQLTemplates.OfflineVersion ver = await getConnection ().Table<SQLTemplates.OfflineVersion> ().FirstOrDefaultAsync ();
			if (ver == null) {
				ver = new SQLTemplates.OfflineVersion ();
				ver.currentVersion = databaseSchemaVersion;
				await updateDBSchemaVersion (ver.currentVersion);
			}
			int newVersion = onUpgrade (ver.currentVersion, databaseSchemaVersion);
			return newVersion;
		}


		private SQLiteAsyncConnection getConnection(){
			if (dbConnection == null) {
				var connectionFactory = new Func<SQLiteConnectionWithLock>(()=>new SQLiteConnectionWithLock(platform, new SQLiteConnectionString(this.dbpath, storeDateTimeAsTicks: false)));
				dbConnection = new SQLiteAsyncConnection (connectionFactory);
			}
			return dbConnection;
		}


		/// <summary>
		/// Gets the DB schema version.
		/// </summary>
		/// <returns>The DB schema version.</returns>
		public async Task<SQLTemplates.OfflineVersion> getDBSchemaVersion (){
			SQLTemplates.OfflineVersion ver =  await getConnection ().Table<SQLTemplates.OfflineVersion> ().FirstOrDefaultAsync ();
			return ver;
		}

		/// <summary>
		/// Updates the DB schema version.
		/// </summary>
		/// <returns>The DB schema version.</returns>
		/// <param name="newVersion">New version.</param>
		public async Task<int> updateDBSchemaVersion (int newVersion){
			SQLTemplates.OfflineVersion ver = new SQLTemplates.OfflineVersion ();
			ver.currentVersion = newVersion;

			await getConnection().InsertAsync (ver);
			return 0;
		}

		private int onUpgrade(int currentVersion, int newVersion){
			while (currentVersion < newVersion) {
				//if (currentVersion == 1){
				//upgrade to 2
				//}

				currentVersion++;
			}

			return currentVersion;
		}

		/// <summary>
		/// Clears the storage.
		/// </summary>
		public void clearStorage(){
			//TODO
		}


		/// <summary>
		/// Gets the database helper.
		/// </summary>
		/// <returns>The database helper.</returns>
		/// <typeparam name="T">The type of entities stored in this collection.</typeparam>
//		public static DatabaseHelper<T> getDatabaseHelper<T>(){
//			return SQLiteHelper<T>.getInstance (platform, dbpath);
//		}

		public ICache<T> GetCache<T> (string collectionName) {
			return new SQLiteCache<T> (collectionName, dbConnection);
		}


		/// <summary>
		/// Gets the collection tables.
		/// </summary>
		/// <returns>The collection tables.</returns>
		public async Task<List<string>> getCollectionTablesAsync ()
		{
			List<SQLTemplates.TableItem> result = await dbConnection.Table<SQLTemplates.TableItem> ().OrderByDescending (t => t.name).ToListAsync ();
			List<string> collections = new List<string> ();


			foreach (SQLTemplates.TableItem item in result) {
				collections.Add (item.name);
			}

			return collections;
		}
	}
}
