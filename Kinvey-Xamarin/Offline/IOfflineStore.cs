﻿using System;
using Kinvey.DotNet.Framework.Core;
using SQLite.Net.Interop;

namespace KinveyXamarin
{

	/// <summary>
	/// This class defines the behaivor of an offline store, which mimics CRUD operations
	/// </summary>
	public interface IOfflineStore<T> {

		ISQLitePlatform platform {get; set;}
		string dbpath{ get; set;}

		T executeGet(AbstractKinveyClient client, AppData<T> appdata, AbstractKinveyOfflineClientRequest<T> request);

		T executeSave(AbstractKinveyClient client, AppData<T> appdata, AbstractKinveyOfflineClientRequest<T> request);

		KinveyDeleteResponse executeDelete(AbstractKinveyClient client, AppData<T> appdata, AbstractKinveyOfflineClientRequest<T> request);

		void insertEntity(AbstractKinveyClient client, AppData<T> appdata, T entity);

		void clearStorage();
	}
}
