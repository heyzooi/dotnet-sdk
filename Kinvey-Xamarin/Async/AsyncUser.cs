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
		/// Login (and create) an new kinvey user without any specified details.
		/// </summary>
		/// <returns>The async task.</returns>
		public async Task<User> LoginAsync(){
			return await base.LoginBlocking ().ExecuteAsync ();
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
		/// Login with a specified username and password.
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="username">Username.</param>
		/// <param name="password">Password.</param>
		public async Task<User> LoginAsync(string username, string password){
			return await base.LoginBlocking (username, password).ExecuteAsync ();
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
		/// Login with a Kinvey Auth Token directly.
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="userId">The _id of the current user.</param>
		/// <param name="authToken">The user's Kinvey Auth Token..</param>
		public async Task<User> LoginKinveyAuthTokenAsync(string userid, string authtoken){
			return await base.LoginKinveyAuthTokenBlocking (userid, authtoken).ExecuteAsync();
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
		/// Login with a third party identity
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="identity">The Third party identity.</param>
		public async Task<User> LoginAsync(ThirdPartyIdentity identity){
			return await base.LoginBlocking (identity).ExecuteAsync();
		}

		/// <summary>
		/// Login with Facebook Credentials
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="accessToken">Facebook Access token.</param>
		public async Task<User> LoginFacebookAsync(string accessToken){
			Provider provider = new Provider ();
			provider.facebook = new FacebookCredential (accessToken);
			return await LoginAsync(new ThirdPartyIdentity(provider));
		}

		/// <summary>
		/// Login with Twitter Credentials
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="accesstoken">Twitter Accesstoken.</param>
		/// <param name="accesstokensecret">Twitter Accesstokensecret.</param>
		/// <param name="consumerkey">Twitter Consumerkey.</param>
		/// <param name="consumersecret">Twitter Consumersecret.</param>
		public async Task<User> LoginTwitterAsync(string accesstoken, string accesstokensecret, string consumerkey, string consumersecret){
			Provider provider = new Provider ();
			provider.twitter = new TwitterCredential (accesstoken, accesstokensecret, consumerkey, consumersecret);
			return await LoginAsync(new ThirdPartyIdentity(provider));
		}

		/// <summary>
		/// Login with Google Credentials
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="accessToken">Google Access token.</param>
		public async Task<User> LoginGoogleAsync(string accessToken){
			Provider provider = new Provider ();
			provider.google = new GoogleCredential (accessToken);
			return await LoginAsync(new ThirdPartyIdentity(provider));
		}

		/// <summary>
		/// Login with LinkedIn Credentials
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="accesstoken">Linkedin Accesstoken.</param>
		/// <param name="accesstokensecret">Linkedin Accesstokensecret.</param>
		/// <param name="consumerkey">Linkedin Consumerkey.</param>
		/// <param name="consumersecret">Linkedin Consumersecret.</param>
		public async Task<User> LoginLinkedinAsync(string accesstoken, string accesstokensecret, string consumerkey, string consumersecret){
			Provider provider = new Provider ();
			provider.linkedin = new LinkedInCredential (accesstoken, accesstokensecret, consumerkey, consumersecret);
			return await LoginAsync(new ThirdPartyIdentity(provider));
		}

		/// <summary>
		/// Login with Auth Link Credentials
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="accesstoken">Auth Link Accesstoken.</param>
		/// <param name="refreshtoken">Auth Link Refreshtoken.</param>
		public async Task<User> LoginAuthlinkAsync(string accesstoken, string refreshtoken){
			Provider provider = new Provider ();
			provider.authlink = new AuthLinkCredential (accesstoken, refreshtoken);
			return await LoginAsync(new ThirdPartyIdentity(provider));
		}

		/// <summary>
		/// Login with Salesforce Credentials
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="access">Salesforce Access.</param>
		/// <param name="reauth">Salesforce Reauth.</param>
		/// <param name="clientid">Salesforce Clientid.</param>
		/// <param name="id">Salesforce Identifier.</param>
		public async Task<User> LoginSalesforceAsync(string access, string reauth, string clientid, string id){
			Provider provider = new Provider ();
			provider.salesforce = new SalesforceCredential (access, reauth, clientid, id);
			return await LoginAsync(new ThirdPartyIdentity(provider));
		}

		/// <summary>
		/// Login with MIC Credentials
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="accessToken">MIC Access token.</param>
		public async Task<User> LoginMICAsync(string accessToken){
			Provider provider = new Provider ();
			provider.kinveyAuth = new MICCredential (accessToken);
			return await LoginAsync(new ThirdPartyIdentity(provider));
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

		/// <summary>
		/// Create a new Kinvey user, with the specified username and password.
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="userid">the username.</param>
		/// <param name="password">the password.</param>
		public async Task<User> CreateAsync(string username, string password){
			return await base.CreateBlocking (username, password).ExecuteAsync ();
		}

		/// <summary>
		/// Retrieve the specified User
		/// </summary>
		/// <param name="userid">Userid.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
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

		/// <summary>
		/// Retrieve the specified User
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="userid">Userid.</param>
		public async Task<User> RetrieveAsync(string userid){
			return await base.RetrieveBlocking (userid).ExecuteAsync ();
		}

		public async Task<User> RetrieveAsync(){
			return await RetrieveAsync (this.Id);
		}

		/// <summary>
		/// Resolve the specified query, resolves, resolve_depth, retain to get a set of users
		/// </summary>
		/// <param name="query">Query.</param>
		/// <param name="resolves">Resolves.</param>
		/// <param name="resolve_depth">Resolve depth.</param>
		/// <param name="retain">If set to <c>true</c> retain references.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
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

		/// <summary>
		/// Resolve the specified query, resolves, resolve_depth, retain to get a set of users
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="query">Query.</param>
		/// <param name="resolves">Resolves.</param>
		/// <param name="resolve_depth">Resolve depth.</param>
		/// <param name="retain">If set to <c>true</c> retain references.</param>
		public async Task<User[]> RetrieveAsync(string query, string[] resolves, int resolve_depth, bool retain){
			return await base.RetrieveBlocking(query, resolves, resolve_depth, retain).ExecuteAsync ();
		}

		/// <summary>
		/// Update the current user
		/// </summary>
		/// <param name="delegates">Delegates for success or failure.</param>
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

		/// <summary>
		/// Updates the current user.
		/// </summary>
		/// <returns>The async task.</returns>
		public async Task<User> UpdateAsync(){
			return await base.UpdateBlocking(this).ExecuteAsync ();
		}

		/// <summary>
		/// Update the specified user
		/// </summary>
		/// <param name="user">User.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
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

		/// <summary>
		/// Updates the specified user.
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="user">User.</param>
		public async Task<User> UpdateAsync(User user){
			return await base.UpdateBlocking(user).ExecuteAsync ();
		}

		/// <summary>
		/// Resets the password for the specified user id
		/// </summary>
		/// <param name="userid">Userid.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
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

		/// <summary>
		/// Resets the password for the specified user id
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="userid">Userid.</param>
		public async Task<User> ResetPasswordAsync(string userid){
			return await base.ResetPasswordBlocking(userid).ExecuteAsync ();
		}

		/// <summary>
		/// Delete the specified userid, with a flag for hard delete
		/// </summary>
		/// <param name="userid">Userid.</param>
		/// <param name="hard">If set to <c>true</c> the user will be permanently deleted.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
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

		/// <summary>
		/// Delete the specified userid, with a flag for hard delete
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="userid">Userid.</param>
		/// <param name="hard">If set to <c>true</c> the user will be permanently deleted.</param>
		public async Task<KinveyDeleteResponse> DeleteAsync(string userid, bool hard){
			return await base.DeleteBlocking(userid, hard).ExecuteAsync ();
		}

		/// <summary>
		/// Sends a verification email
		/// </summary>
		/// <param name="userid">Userid.</param>
		/// <param name="delegates">Delegates for success or failure.</param>
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

		/// <summary>
		/// Sends a verification email
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="userid">Userid.</param>
		public async Task<User> EmailVerificationAsync(string userid){
			return await base.EmailVerificationBlocking(userid).ExecuteAsync ();
		}
			
	}
}

