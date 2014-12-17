﻿using System;
using System.Threading.Tasks;
using KinveyUtils;

namespace KinveyXamarin
{
	/// <summary>
	/// Async user.  This class allows access to Kinvey's User API asynchronously.  
	/// </summary>
	public class AsyncUser: User
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KinveyXamarin.AsyncUser"/> class.
		/// </summary>
		/// <param name="client">A configured instance of a Kinvey client.</param>
		/// <param name="client">A configured AuthRequest Builder, containing information about the upcoming login request.</param>
		public AsyncUser (AbstractClient client, KinveyAuthRequest.Builder builder) : base(client, builder)
		{
		}

		/// <summary>
		/// Login (and create) an new kinvey user without any specified details.
		/// </summary>
		/// <param name="delegates">Delegates for success or failure.</param>
		public void Login(KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.LoginBlocking().Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		/// <summary>
		/// Login with a specified username and password.
		/// </summary>
		/// <param name="username">The username.</param>
		/// <param name="password">The password.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
		public void Login(string username, string password, KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.LoginBlocking(username, password).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		/// <summary>
		/// Login with a credential object.
		/// </summary>
		/// <param name="cred">The crendential to login with.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
		public void Login(Credential cred, KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.LoginBlocking(cred).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		/// <summary>
		/// Login with a Kinvey Auth Token directly.
		/// </summary>
		/// <param name="userId">The _id of the current user.</param>
		/// <param name="authToken">The user's Kinvey Auth Token..</param>
		/// <param name="delegates">Delegates for success or failure.</param>
		public void LoginKinveyAuthToken(string userId, string authToken, KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.LoginBlocking(userId, authToken).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}


		/// <summary>
		/// Login with a third party identity
		/// </summary>
		/// <param name="identity">The Third party identity.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
		public void Login(ThirdPartyIdentity identity, KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.LoginBlocking(identity).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		/// <summary>
		/// Logout the current user.
		/// </summary>
		public void Logout()
		{
			Task.Run (() => {
				try{
					base.logoutBlocking().Execute();
//					delegates.onSuccess(default(User)); //TODO find a better way, logout has no return value and void is not nullable in c#
				}catch(Exception e){
//					delegates.onError(e);
					Logger.Log(e);
				}
			});
		}

		/// <summary>
		/// Create a new Kinvey user, with the specified username and password.
		/// </summary>
		/// <param name="userid">the username.</param>
		/// <param name="password">the password.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
		public void Create(string username, string password, KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.CreateBlocking(username, password).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}


		public void Retrieve(string userid, KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.RetrieveBlocking(userid).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		public void Retrieve(string query, string[] resolves, int resolve_depth, bool retain, KinveyDelegate<User[]> delegates)
		{
			Task.Run (() => {
				try{
					User[] user = base.RetrieveBlocking(query, resolves, resolve_depth, retain).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		public void Update(KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.UpdateBlocking(this).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		public void Update(User user, KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User us = base.UpdateBlocking(user).Execute();
					delegates.onSuccess(us);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		public void ResetPassword(string userid, KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.ResetPasswordBlocking(userid).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		public void Delete(string userid, bool hard, KinveyDelegate<KinveyDeleteResponse> delegates)
		{
			Task.Run (() => {
				try{
					KinveyDeleteResponse del = base.DeleteBlocking(userid, hard).Execute();
					delegates.onSuccess(del);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}

		public void EmailVerification(string userid, KinveyDelegate<User> delegates)
		{
			Task.Run (() => {
				try{
					User user = base.EmailVerificationBlocking(userid).Execute();
					delegates.onSuccess(user);
				}catch(Exception e){
					delegates.onError(e);
				}
			});
		}
			
	}
}

