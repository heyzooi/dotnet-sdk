﻿using System;
using System.Threading.Tasks;
using NUnit.Framework;
using KinveyXamarin;
using Newtonsoft.Json.Linq;

namespace UnitTestFramework
{
	[TestFixture]
	public class TestClient
	{
		[SetUp]
		public void Setup ()
		{
		}

		[TearDown]
		public void Tear ()
		{
		}

		[Test]
		public async Task TestClientBuilderBasic()
		{
			// Arrange
			const string url = "https://baas.kinvey.com/";
			Client.Builder builder = new Client.Builder(TestSetup.app_key, TestSetup.app_secret);

			// Act
			Client client = await builder.Build();

			// Assert
			Assert.False(client == null);
			Assert.False(string.IsNullOrEmpty(client.BaseUrl));
			Assert.True(string.Equals(client.BaseUrl, url));
		}

		[Test]
		public void TestClientBuilderBasicBad()
		{
			// Arrange

			// Act
			// Assert
			Assert.Catch (delegate () {
				DataStore<ToDo> todoStore = DataStore<ToDo>.Collection("ToDos", DataStoreType.NETWORK);
			});
		}

		[Test]
		public async Task TestClientBuilderSetValues()
		{
			// Arrange
			Client.Builder builder = new Client.Builder(TestSetup.app_key, TestSetup.app_secret);

			// Act
			builder.setFilePath("")
				.setLogger(delegate(string msg) { Console.WriteLine(msg); });

			// Assert
			Client client = await builder.Build();

			Assert.False(client == null);
			Assert.False(string.IsNullOrEmpty(client.BaseUrl));
			Assert.False(client.Store == null);
			Assert.False(client.logger == null);
			Assert.False(string.IsNullOrEmpty(client.MICHostName));
		}

		[Test]
		public async Task TestClientBuilderSetOrgID()
		{
			// Arrange
			const string TEST_ORG = "testOrg";
			Client.Builder builder = new Client.Builder(TestSetup.app_key, TestSetup.app_secret);

			// Act
			builder.SetOrganizationID(TEST_ORG);
			Client c = await builder.Build();

			// Assert
			Assert.True(c.OrganizationID.Equals(TEST_ORG));
		}

		[Test]
		public async Task TestClientBuilderDoNotSetOrgID()
		{
			// Arrange
			const string TEST_ORG = "testOrg";
			Client.Builder builder = new Client.Builder(TestSetup.app_key, TestSetup.app_secret);

			// Act
			Client c = await builder.Build();

			// Assert
			Assert.False(c.OrganizationID.Equals(TEST_ORG));
			Assert.True(c.OrganizationID.Equals(TestSetup.app_key));
		}

		[Test]
		public void ClientBuilderSetBaseURL()
		{
			// Arrange
			const string url = "https://www.test.com/";
			Client.Builder builder = new Client.Builder(TestSetup.app_key, TestSetup.app_secret);

			// Act
			builder.setBaseURL(url);

			// Assert
			Assert.False(string.IsNullOrEmpty(builder.BaseUrl));
			Assert.True(string.Equals(builder.BaseUrl, url));
		}

		[Test]
		public void ClientBuilderSetBaseURLBad()
		{
			// Arrange
			Client.Builder builder = new Client.Builder(TestSetup.app_key, TestSetup.app_secret);

			// Act
			// Assert
			Assert.Catch( delegate() {
				builder.setBaseURL("www.test.com");
			});
		}

		[Test]
		public async Task TestClientPingAsync()
		{
			// Arrange
			Client.Builder builder = new Client.Builder(TestSetup.app_key, TestSetup.app_secret);
			Client client = await builder.Build();

			// Act
			PingResponse pr = await client.PingAsync();

			// Assert
			Assert.IsNotNull(pr.kinvey);
			Assert.IsNotEmpty(pr.kinvey);
			Assert.True(pr.kinvey.StartsWith("hello"));
			Assert.IsNotNull(pr.version);
			Assert.IsNotEmpty(pr.version);
		}

		[Test]
		public async Task TestClientPingAsyncBad()
		{
			// Arrange
			Client.Builder builder = new Client.Builder(TestSetup.app_key_fake, TestSetup.app_secret_fake);
			Client client = await builder.Build();

			// Act
			PingResponse pr = await client.PingAsync();

			// Assert
			Assert.IsNotNull(pr);
			Assert.IsNull(pr.kinvey);
			Assert.IsNull(pr.version);
		}

		[Test]
		public async Task TestCustomEndpoint()
		{
			// Arrange
			new Client.Builder(TestSetup.app_key, TestSetup.app_secret)
				.setFilePath(TestSetup.db_dir)
				.setOfflinePlatform(new SQLite.Net.Platform.Generic.SQLitePlatformGeneric())
				.Build();

			if (!Client.SharedClient.IsUserLoggedIn())
			{
				await User.LoginAsync(TestSetup.user, TestSetup.pass);
			}

			// Act
			JObject obj = new JObject();
			obj.Add("input", 1);

			CustomEndpoint<JObject, ToDo[]> ce = Client.SharedClient.CustomEndpoint<JObject, ToDo[]>();
			var result = await ce.ExecuteCustomEndpoint("test", obj);
			string outputstr= result[1].DueDate;
			int output = int.Parse(outputstr);

			// Assert
			Assert.AreEqual(3, output);
			Assert.AreEqual(2, result.Length);

			// Teardown
			Client.SharedClient.ActiveUser.Logout();
		}
	}
}
