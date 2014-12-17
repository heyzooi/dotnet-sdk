﻿// Copyright (c) 2014, Kinvey, Inc. All rights reserved.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KinveyXamarin
{
	/// <summary>
	/// This class manages the state of a Kinvey user.  User methods can be accessed through this class, and this class represents the currently logged in user.
	/// </summary>
	[JsonObject(MemberSerialization.OptOut)]
    public class User : JObject
    {
		/// <summary>
		/// The name of the user collection.
		/// </summary>
        public const string UserCollectionName = "user";

		/// <summary>
		/// the available login types
		/// </summary>
        public enum LoginType 
        {
            IMPLICIT,
            KINVEY,
            CREDENTIALSTORE,
			THIRDPARTY
        }

		/// <summary>
		/// The _id.
		/// </summary>
        [JsonProperty("_id")]
        private String id;

		/// <summary>
		/// The auth token.
		/// </summary>
        private String authToken;

		/// <summary>
		/// The username.
		/// </summary>
        [JsonProperty("username")]
        private String username;

		/// <summary>
		/// The client.
		/// </summary>
        private AbstractClient client;

		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		/// <value>The identifier.</value>
        public string Id
        {
            get { return this.id; }
            set { this.id = value; }
        }

		/// <summary>
		/// Gets or sets the auth token.
		/// </summary>
		/// <value>The auth token.</value>
        public string AuthToken
        {
            get { return this.authToken; }
            set { this.authToken = value; }
        }

		/// <summary>
		/// Gets or sets the name of the user.
		/// </summary>
		/// <value>The name of the user.</value>
        public string UserName
        {
            get { return this.username; }
            set { this.username = value; }
        }

		/// <summary>
		/// Gets the kinvey client.
		/// </summary>
		/// <value>The kinvey client.</value>
        public AbstractClient KinveyClient
        {
            get { return this.client; }
        }

		/// <summary>
		/// the auth request builder.
		/// </summary>
        private KinveyAuthRequest.Builder builder;

		/// <summary>
		/// The type of user
		/// </summary>
		private LoginType type {get; set;}


		/// <summary>
		/// Initializes a new instance of the <see cref="KinveyXamarin.User"/> class.
		/// </summary>
		/// <param name="client">Client.</param>
		/// <param name="builder">Builder.</param>
        public User(AbstractClient client, KinveyAuthRequest.Builder builder) 
        {
            this.client = client;
            this.builder = builder;
            builder.KinveyUser = this;
        }
		/// <summary>
		/// Initializes a new instance of the <see cref="KinveyXamarin.User"/> class.
		/// </summary>
        public User() { }

		/// <summary>
		/// checks if there is currently a logged in user.
		/// </summary>
		/// <returns><c>true</c>, if user logged in was ised, <c>false</c> otherwise.</returns>
        public bool isUserLoggedIn()
        {
            return (this.id != null || this.AuthToken != null || this.UserName != null);
        }

		/// <summary>
		/// Inits the user from a kinvey auth response.
		/// </summary>
		/// <returns>The user.</returns>
		/// <param name="response">Response.</param>
		/// <param name="userType">User type.</param>
        private User InitUser(KinveyAuthResponse response, string userType) 
        {
            this.id = response.UserId;
            // TODO process Unknown keys
            // this.put("_kmd", response.getMetadata());
            // this.putAll(response.getUnknownKeys());

            //this.username = response
            this.AuthToken = response.AuthToken;
            CredentialManager credentialManager = new CredentialManager(KinveyClient.Store);
            ((KinveyClientRequestInitializer) KinveyClient.RequestInitializer).KinveyCredential = credentialManager.CreateAndStoreCredential(response, this.id);
            KinveyClient.ClientUsers.AddUser(this.id, userType);
            KinveyClient.ClientUsers.CurrentUser = this.id;
            return this;
        }

		/// <summary>
		/// Inits the user from a credential
		/// </summary>
		/// <returns>The user.</returns>
		/// <param name="credential">Credential.</param>
        private User InitUser(Credential credential)
        {
            this.id = credential.UserId;
            this.AuthToken = credential.AuthToken;
            return this;
        }

		/// <summary>
		/// Removes the user from the store..
		/// </summary>
		/// <param name="userID">User _id.</param>
        private void removeFromStore(string userID)
        {
            CredentialManager credentialManager = new CredentialManager(KinveyClient.Store);
            credentialManager.RemoveCredential(userID);
        }

		/// <summary>
		/// Logins an anonymous user synchronously.
		/// </summary>
		/// <returns>The blocking.</returns>
		public LoginRequest LoginBlocking()
        {
			this.type = LoginType.IMPLICIT;
			return new LoginRequest(this).buildAuthRequest();
        }

		/// <summary>
		/// Logins a user with a username and password synchronously.
		/// </summary>
		/// <returns>The blocking.</returns>
		/// <param name="username">Username.</param>
		/// <param name="password">Password.</param>
		public LoginRequest LoginBlocking(string username, string password)
        {
			this.type = LoginType.KINVEY;
			return new LoginRequest(username, password, false, this).buildAuthRequest();
        }

		/// <summary>
		/// Logins a user with a credential synchronously
		/// </summary>
		/// <returns>The blocking.</returns>
		/// <param name="cred">Cred.</param>
		public LoginRequest LoginBlocking(Credential cred) 
        {
			this.type = LoginType.CREDENTIALSTORE;
			return new LoginRequest (cred, this).buildAuthRequest ();
        }

		public LoginRequest LoginBlocking(ThirdPartyIdentity identity){
			this.type = LoginType.THIRDPARTY;
			return new LoginRequest (identity, this);
		}

		/// <summary>
		/// Logins a user with a Kinvey Auth token synchronously.
		/// </summary>
		/// <returns>The kinvey auth token blocking.</returns>
		/// <param name="userId">User identifier.</param>
		/// <param name="authToken">Auth token.</param>
		public LoginRequest LoginKinveyAuthTokenBlocking(string userId, string authToken) 
        {
            this.AuthToken = authToken;
            this.id = userId;
			Credential c = Credential.From (this);
			return LoginBlocking(c);
        }

		/// <summary>
		/// Logouts the user synchronously.
		/// </summary>
		/// <returns>The blocking.</returns>
		public LogoutRequest logoutBlocking() 
        {
            return new LogoutRequest(this.KinveyClient.Store, this);
        }


		public RetrieveRequest RetrieveBlocking(string userid){

			var urlParameters = new Dictionary<string, string>();
			urlParameters.Add("appKey", ((KinveyClientRequestInitializer)client.RequestInitializer).AppKey);
			urlParameters.Add("userID", userid);
			RetrieveRequest retrieve = new RetrieveRequest(client, userid, urlParameters);
			client.InitializeRequest(retrieve);

			return retrieve;
		}
			
		public RetrieveUsersRequest RetrieveBlocking(string query, string[] resolves, int resolve_depth, bool retain){

			var urlParameters = new Dictionary<string, string>();
			urlParameters.Add("appKey", ((KinveyClientRequestInitializer)client.RequestInitializer).AppKey);

			urlParameters.Add("query", query);
		
			urlParameters.Add("resolve", string.Join(",", resolves));
			urlParameters.Add("resolve_depth", resolve_depth > 0 ? resolve_depth.ToString() : "1");
			urlParameters.Add("retainReferences",  retain.ToString());



			RetrieveUsersRequest retrieve = new RetrieveUsersRequest (client, query, urlParameters);

			client.InitializeRequest(retrieve);

			return retrieve;


		}


		public UpdateRequest UpdateBlocking(User u){
			var urlParameters = new Dictionary<string, string>();
			urlParameters.Add("appKey", ((KinveyClientRequestInitializer)client.RequestInitializer).AppKey);
			urlParameters.Add("userID", u.id);

			UpdateRequest update = new UpdateRequest (client, u, urlParameters);

			client.InitializeRequest(update);

			return update;


		}

		public ResetPasswordRequest ResetPasswordBlocking(string userid){
			var urlParameters = new Dictionary<string, string>();
			urlParameters.Add("appKey", ((KinveyClientRequestInitializer)client.RequestInitializer).AppKey);
			urlParameters.Add("userID", userid);

			ResetPasswordRequest reset = new ResetPasswordRequest (client, userid, urlParameters);

			client.InitializeRequest(reset);

			return reset;

		}

		public DeleteRequest DeleteBlocking(string userid, bool hard){
			var urlParameters = new Dictionary<string, string>();
			urlParameters.Add("appKey", ((KinveyClientRequestInitializer)client.RequestInitializer).AppKey);
			urlParameters.Add("userID", userid);
			urlParameters.Add ("hard", hard.ToString());

			DeleteRequest delete = new DeleteRequest (client, userid, hard, urlParameters);

			client.InitializeRequest(delete);

			return delete;		
		}

		public EmailVerificationRequest EmailVerificationBlocking(string userid){
			var urlParameters = new Dictionary<string, string>();
			urlParameters.Add("appKey", ((KinveyClientRequestInitializer)client.RequestInitializer).AppKey);
			urlParameters.Add("userID", userid);

			EmailVerificationRequest email = new EmailVerificationRequest (client, userid, urlParameters);

			client.InitializeRequest(email);

			return email;
		}
			

		/// <summary>
		/// Creates the User with a blocking implementation.
		/// </summary>
		/// <returns>The created User.</returns>
		/// <param name="userid">the username of the user.</param>
		/// <param name="password">the password for the user.</param>
		public LoginRequest CreateBlocking(string username, string password) 
        {
			this.type = LoginType.KINVEY;
			return new LoginRequest(username, password, true, this).buildAuthRequest();
        }


		/// <summary>
		/// A synchronous login request.
		/// </summary>
		public class LoginRequest 
        {
            Credential credential;
            LoginType type;
            KinveyAuthRequest request;
            User memberUser;


			/// <summary>
			/// Initializes a new instance of the <see cref="Kinvey.DotNet.Framework.User+LoginRequest"/> class.
			/// </summary>
			/// <param name="user">User.</param>
			public LoginRequest(User user) 
            {
                memberUser = user;
				memberUser.builder.Create = true;
				this.type = user.type;
            }

			/// <summary>
			/// Initializes a new instance of the <see cref="Kinvey.DotNet.Framework.User+LoginRequest"/> class.
			/// </summary>
			/// <param name="username">Username.</param>
			/// <param name="password">Password.</param>
			/// <param name="setCreate">If set to <c>true</c> set create.</param>
			/// <param name="user">User.</param>
			public LoginRequest(string username, string password, bool setCreate, User user) 
            {
                this.memberUser = user;
				memberUser.builder.Username = username;
				memberUser.builder.Password = password;
				memberUser.builder.Create = setCreate;
				memberUser.builder.KinveyUser = user;
				this.type = user.type;
            }

			/// <summary>
			/// Initializes a new instance of the <see cref="Kinvey.DotNet.Framework.User+LoginRequest"/> class.
			/// </summary>
			/// <param name="credential">Credential.</param>
			/// <param name="user">User.</param>
			public LoginRequest(Credential credential, User user) 
            {
                this.memberUser = user;
                this.credential = credential;
				this.type = user.type;
            }

			public LoginRequest(ThirdPartyIdentity identity, User user){
				this.memberUser = user;
				this.memberUser.builder.Identity = identity;
				this.type = user.type;
			}

			/// <summary>
			/// Builds the auth request.
			/// </summary>
			/// <returns>The auth request.</returns>
            public LoginRequest buildAuthRequest() {
                this.request = memberUser.builder.build();
                return this;
            }



//			public async Task<User> ExecuteAsync() 
//            {
//                if (memberUser.isUserLoggedIn())
//                {
//                    throw new KinveyException("Attempting to login when a user is already logged in",
//                            "call `myClient.user().logout().execute() first -or- check `myClient.user().isUserLoggedIn()` before attempting to login again",
//                            "Only one user can be active at a time, and logging in a new user will replace the current user which might not be intended");
//                }
//                string userType = "";
//                if (this.type == LoginType.CREDENTIALSTORE) 
//                {
//                    return memberUser.initUser(credential);
//                }
//                else 
//                {
//                    switch (this.type)
//                    {
//                        case LoginType.IMPLICIT:
//                            userType = "Implicit";
//                            break;
//                        case LoginType.KINVEY:
//                            userType = "Kinvey";
//                            break;
//                        default:
//                            throw new ArgumentException("Invalid LoginType operation.");
//                    }
//                }
////				this.request.RequestAuth = new HttpBasicAuthenticator(AppKey, AppSecret);
//
////				client.InitializeRequest(this.request);
////				this.request.
//
//				KinveyAuthResponse response = await this.request.ExecuteAsync();
//                return memberUser.InitUser(response, userType);
//            }

			/// <summary>
			/// Execute this instance.
			/// </summary>
			public User Execute() 
			{
				if (memberUser.isUserLoggedIn())
				{
					throw new KinveyException("Attempting to login when a user is already logged in",
						"call `myClient.user().logout().execute() first -or- check `myClient.user().isUserLoggedIn()` before attempting to login again",
						"Only one user can be active at a time, and logging in a new user will replace the current user which might not be intended");
				}
				string userType = "";
				if (this.type == LoginType.CREDENTIALSTORE) 
				{
					return memberUser.InitUser(credential);
				}
				else 
				{
					userType = this.type.ToString ();
				}
				KinveyAuthResponse response = this.request.Execute();
				return memberUser.InitUser(response, userType);
			}
        }

		/// <summary>
		/// A synchronous logout request.
		/// </summary>
        public class LogoutRequest
        {

            private ICredentialStore store;
            private User memberUser;

            public LogoutRequest(ICredentialStore store, User user)
            {
                this.memberUser = user;
                this.store = store;
            }

            public void Execute()
            {
                CredentialManager manager = new CredentialManager(this.store);
                manager.RemoveCredential(memberUser.id);
                memberUser.KinveyClient.CurrentUser = null;
                ((KinveyClientRequestInitializer)memberUser.KinveyClient.RequestInitializer).KinveyCredential = null;
            }
        }
			
		/// <summary>
		/// Deletes the user with the specified _id.
		/// </summary>
		[JsonObject(MemberSerialization.OptIn)]
		public  class DeleteRequest : AbstractKinveyClientRequest<KinveyDeleteResponse> {
			private const string REST_PATH = "user/{appKey}/{userID}?hard={hard}";

			[JsonProperty]
			public bool hard = false;

			[JsonProperty]
			public string userID;

			public DeleteRequest(AbstractClient client, string userID, bool hard, Dictionary<string, string> urlProperties) :
			base(client, "DELETE", REST_PATH, default(KinveyDeleteResponse), urlProperties){
				this.userID = userID;
				this.hard = hard;
			}

			public override KinveyDeleteResponse Execute() {
				KinveyDeleteResponse resp = base.Execute();
//				this.logout();

				return resp;
			}
		}

		/// <summary>
		/// Retrieve a user
		/// </summary>
		public class RetrieveRequest : AbstractKinveyClientRequest<User> {
			private const string REST_PATH = "user/{appKey}/{userID}";

			[JsonProperty]
			public string userID;

			public User user;

			public RetrieveRequest(AbstractClient client, string userID, Dictionary<string, string> urlProperties) :
			base(client, "GET", REST_PATH, default(User), urlProperties) {
				this.userID = userID;
			}				
		}

		/// <summary>
		/// Retrieve users.
		/// </summary>
		public class RetrieveUsersRequest : AbstractKinveyClientRequest<User[]> {
			private const string REST_PATH = "user/{appKey}/{?query,resolve,resolve_depth,retainReference}";
		
			[JsonProperty("query")]
			public string queryFilter;

			[JsonProperty("resolve")]
			public string resolve;
			[JsonProperty("resolve_depth")]
			public string resolve_depth;
			[JsonProperty("retainReferences")]
			public string retainReferences;

			public RetrieveUsersRequest(AbstractClient client, string query, Dictionary<string, string> urlProperties):
			base(client, "GET", REST_PATH, default(User[]), urlProperties){
				this.queryFilter = query;
			}
		}

		/// <summary>
		/// Update a user
		/// </summary>
		public class UpdateRequest : AbstractKinveyClientRequest<User> {
			private const string REST_PATH = "user/{appKey}/{userID}";

			[JsonProperty]
			public string userID;

			private User user;

			public UpdateRequest(AbstractClient client, User user, Dictionary<string, string> urlProperties) :
			base(client, "PUT", REST_PATH, user, urlProperties){
				this.userID = user.id;
				this.user = user;

			}

			public override User Execute(){

				User u = base.Execute();

				if (u.id == (user.id)){
					KinveyAuthResponse auth = new KinveyAuthResponse();

					auth.Add("_id", u["_id"]);
					KinveyAuthResponse.KinveyUserMetadata kmd = new KinveyAuthResponse.KinveyUserMetadata();
					kmd.Add("lmt", u["_kmd.lmt"]) ;
					kmd.Add("authtoken", u["_kmd.authtoken"]);
					kmd.Add("_kmd", u["_kmd"]);
					auth.Add("_kmd", kmd);
					auth.Add("username", u["username"]);
//					for (Object key : u.keySet()){
//						if (!key.toString().equals("_kmd")){
//							auth.put(key.toString(), u.get(key));
//						}
//					}

					string utype = user.type.ToString();
				
					return this.user.InitUser(auth, utype);
				}else{
					return u;
				}
			}


		}

		/// <summary>
		/// Reset password.
		/// </summary>
		public class ResetPasswordRequest : AbstractKinveyClientRequest<User> {
			private const string REST_PATH = "/rpc/{appKey}/{userID}/user-password-reset-initiate";

			[JsonProperty]
			public string userID;

			public ResetPasswordRequest(AbstractClient client, string userid, Dictionary<string, string> urlProperties) :
			base(client, "POST", REST_PATH, default(User), urlProperties){
				this.userID = userid;
				this.RequireAppCredentials = true;

			}
		}

		/// <summary>
		/// Email verification.
		/// </summary>
		public class EmailVerificationRequest : AbstractKinveyClientRequest<User> {
			private const string REST_PATH = "rpc/{appKey}/{userID}/user-email-verification-initiate";

			[JsonProperty]
			public string userID;

			public EmailVerificationRequest(AbstractClient client, string userID, Dictionary<string, string> urlProperties) :
			base(client, "POST", REST_PATH, default(User), urlProperties){
				this.userID = userID;
				this.RequireAppCredentials = true;
			}
		}
			
    }
}


            