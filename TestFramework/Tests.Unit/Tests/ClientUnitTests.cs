﻿// Copyright (c) 2016, Kinvey, Inc. All rights reserved.
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
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using KinveyXamarin;

namespace TestFramework
{
	[TestFixture]
	public class ClientUnitTests
	{
		[SetUp]
		public void Setup()
		{
		}

		[TearDown]
		public void Tear()
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
			Assert.Catch(delegate ()
			{
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
				.setLogger(delegate (string msg) { Console.WriteLine(msg); });

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
			builder.SetSSOGroupKey(TEST_ORG);
			Client c = await builder.Build();

			// Assert
			Assert.True(c.SSOGroupKey.Equals(TEST_ORG));
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
			Assert.False(c.SSOGroupKey.Equals(TEST_ORG));
			Assert.True(c.SSOGroupKey.Equals(TestSetup.app_key));
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
			Assert.Catch(delegate ()
			{
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
		[Ignore("Placeholder - No unit test yet")]
		public async Task TestCustomEndpoint()
		{
			// Arrange

			// Act

			// Assert
		}
	}
}

