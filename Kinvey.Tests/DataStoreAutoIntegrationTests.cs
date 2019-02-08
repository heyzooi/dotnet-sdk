﻿
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Kinvey.Tests
{
    [TestClass]
    public class DataStoreAutoIntegrationTests : BaseTestClass
    {
        private Client kinveyClient;
        private const string toDosCollection = "ToDos";
        private const string personCollection = "person";
        private const string flashCardCollection = "FlashCard";

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();

            Client.Builder builder = ClientBuilder
                .SetFilePath(TestSetup.db_dir);

            if (MockData)
            {
                builder.setBaseURL("http://localhost:8080");
            }

            kinveyClient = builder.Build();
        }

        private void SetUnReachableRootUrlToKinveyClient()
        {
            var property = kinveyClient.GetType().BaseType.BaseType.GetField("rootUrl", BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(kinveyClient, "http://localhost:12345/");
        }

        #region Collection

        [TestMethod]
        public void TestCollectionWithDefaultParameters()
        {
            // Arrange

            // Act
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection);

            // Assert
            Assert.IsNotNull(todoStore);
            Assert.IsTrue(string.Equals(todoStore.CollectionName, toDosCollection));
            Assert.IsNotNull(todoStore.KinveyClient);
        }

        [TestMethod]
        public void TestCollectionWithParameters()
        {
            // Arrange

            // Act
            DataStore<ToDo> todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO, kinveyClient);

            // Assert
            Assert.IsNotNull(todoStore);
            Assert.IsTrue(string.Equals(todoStore.CollectionName, toDosCollection));
            Assert.AreEqual(todoStore.StoreType, DataStoreType.AUTO);
            Assert.IsNotNull(todoStore.KinveyClient);
        }

        #endregion Collection

        #region Remove by query

        [TestMethod]
        public async Task TestDeleteByQueryStringValueStartsWithExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(7);
            }
           
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 to delete",
                Details = "Delete details1"
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Delete details2"
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Not delete details3"
            };

            var query = autoStore.Where(x => x.Details.StartsWith("Delet"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);

            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            var listToDoNetwork = await autoStore.FindAsync();
            var listToDoCache = await syncStore.FindAsync();

            var existingItemInNetwork1 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInNetwork2 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInNetwork3 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem3.ID);
            var existingItemInCache1 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInCache2 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInCache3 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem3.ID);

            // Teardown
            await autoStore.RemoveAsync(savedItem3.ID);

            // Assert            
            Assert.IsNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNotNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNotNull(existingItemInCache3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryOrExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1"
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                BoolVal = true
            };

            var query = autoStore.Where(e => e.Name == "Task2 to delete" || e.BoolVal == true);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);

            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryBoolValueExplicitEqualsExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1"
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                BoolVal = true
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);

            var query = autoStore.Where(x => x.BoolVal.Equals(true));

            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryBoolValueImplicitEqualExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1"
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                BoolVal = true
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);

            var query = autoStore.Where(x => x.BoolVal);

            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryDateTimeValueGreaterThanExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2017, 4, 20, 19, 56, 0)
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 21, 19, 56, 0)
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 22, 19, 56, 0)
            };

            var endDate = new DateTime(2018, 1, 1, 0, 0, 0);
            var query = autoStore.Where(x => x.NewDate > endDate);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
        
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryDateTimeValueGreaterThanOrEqualExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2017, 4, 20, 19, 56, 0)
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 1, 1, 0, 0, 0)
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 22, 19, 56, 0)
            };

            var endDate = new DateTime(2018, 1, 1, 0, 0, 0);
            var query = autoStore.Where(x => x.NewDate >= endDate);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);

            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryDateTimeValueLessThanExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0)
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 1, 1, 0, 0, 0)
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 22, 19, 56, 0)
            };

            var endDate = new DateTime(2018, 12, 10, 0, 0, 0);
            var query = autoStore.Where(x => x.NewDate < endDate);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);

            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryDateTimeValueLessThanOrEqualExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0)
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0)
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 4, 22, 19, 56, 0)
            };

            var endDate = new DateTime(2018, 12, 10, 0, 0, 0);
            var query = autoStore.Where(x => x.NewDate <= endDate);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
         
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueGreaterThanExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 1
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 2
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 3
            };

            var query = autoStore.Where(x => x.Value > 1);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
            
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueGreaterThanOrEqualExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
 
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 1
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 2
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 3
            };

            var endDate = new DateTime(2018, 12, 10, 0, 0, 0);
            var query = autoStore.Where(x => x.Value >= 2);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
         
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueLessThanExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
          
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 2
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 1
            };

            var endDate = new DateTime(2018, 12, 10, 0, 0, 0);
            var query = autoStore.Where(x => x.Value < 2);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
          
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueLessThanOrEqualExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 2
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 1
            };

            var query = autoStore.Where(x => x.Value <= 2);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);            

            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryIntValueEqualsExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
           
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete",
                Details = "Details3",
                Value = 1
            };

            var query = autoStore.Where(x => x.Value.Equals(1));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);          

            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryLogicalAndExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "TestDetails1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task to delete",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task to delete",
                Details = "Details3",
                Value = 1
            };

            var query = autoStore.Where(todo => todo.Details.StartsWith("Deta") && todo.Name.Equals("Task to delete"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
            
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryLogicalAndWithOrExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "TestDetails1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "TaskDel2",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = autoStore.Where(todo => todo.DueDate.Equals("2018-04-22T19:56:00.963Z") && (todo.Name.StartsWith("TaskDel") ||
                        todo.Details.Equals("Details for")));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
            
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryLogicalOrExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "TestDetails1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del2",
                Details = "Details2",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = autoStore.Where(todo => todo.Name.StartsWith("Task Del") || todo.Details.Equals("Details for"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
         
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryLogicalOrWithAndExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(9);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "TestDetails1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del2",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task3",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = autoStore.Where(todo => todo.Name.StartsWith("Task Del") || todo.DueDate.Equals("2018-04-22T19:56:00.963Z") && todo.Details.Equals("Details for"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
           
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            ToDo existingItemInNetwork1 = null;
            ToDo existingItemInNetwork2 = null;
            ToDo existingItemInNetwork3 = null;
            ToDo existingItemInCache1 = null;
            ToDo existingItemInCache2 = null;
            ToDo existingItemInCache3 = null;

            try
            {
                existingItemInNetwork1 = await autoStore.FindByIDAsync(savedItem1.ID);
                existingItemInCache1 = await syncStore.FindByIDAsync(savedItem1.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork2 = await autoStore.FindByIDAsync(savedItem2.ID);
                existingItemInCache2 = await syncStore.FindByIDAsync(savedItem2.ID);
            }
            catch (Exception)
            {

            }

            try
            {
                existingItemInNetwork3 = await autoStore.FindByIDAsync(savedItem3.ID);
                existingItemInCache3 = await syncStore.FindByIDAsync(savedItem3.ID);
            }
            catch (Exception)
            {

            }

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);

            // Assert            
            Assert.IsNotNull(existingItemInCache1);
            Assert.IsNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(2, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesStartsWithAndEqualsExpressionsAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var networkStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del2",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task Del3",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = autoStore.Where(x => x.Details.StartsWith("Details f")).Where(y => y.Name.StartsWith("Task D")).Where(z => z.DueDate.Equals("2018-04-22T19:56:00.963Z"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
       
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            var listToDoNetwork = await networkStore.FindAsync();
            var listToDoCache = await syncStore.FindAsync();

            var existingItemInNetwork1 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInNetwork2 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInNetwork3 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem3.ID);
            var existingItemInCache1 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInCache2 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInCache3 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem3.ID);

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);
            await autoStore.RemoveAsync(savedItem2.ID);

            // Assert            
            Assert.IsNull(existingItemInCache1);
            Assert.IsNotNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNotNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesEqualsExpressionsAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8);
            }
           
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var networkStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Test",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),
                Value = 1
            };
            var newItem3 = new ToDo
            {
                Name = "Task Test",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = autoStore.Where(x => x.Details.Equals("Details for")).Where(y => y.Name.Equals("Task Test")).Where(z => z.DueDate.Equals("2018-04-22T19:56:00.963Z"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
           
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            var listToDoNetwork = await networkStore.FindAsync();
            var listToDoCache = await syncStore.FindAsync();

            var existingItemInNetwork1 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInNetwork2 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInNetwork3 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem3.ID);
            var existingItemInCache1 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInCache2 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInCache3 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem3.ID);

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);
            await autoStore.RemoveAsync(savedItem2.ID);

            // Assert            
            Assert.IsNull(existingItemInCache1);
            Assert.IsNotNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNotNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesDifferentEqualsExpressionsAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var networkStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = autoStore.Where(x => x.Details == ("Details for")).Where(y => y.BoolVal == true).Where(z => z.DueDate.Equals("2018-04-22T19:56:00.963Z"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
            
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            var listToDoNetwork = await networkStore.FindAsync();
            var listToDoCache = await syncStore.FindAsync();

            var existingItemInNetwork1 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInNetwork2 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInNetwork3 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem3.ID);
            var existingItemInCache1 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInCache2 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInCache3 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem3.ID);

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);
            await autoStore.RemoveAsync(savedItem2.ID);

            // Assert            
            Assert.IsNull(existingItemInCache1);
            Assert.IsNotNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNotNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesFluentSyntaxEqualExpressionsAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var networkStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task1",
                Details = "Details for",
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = from t in autoStore where t.Details == "Details for" where t.Name == "Task Del" where t.DueDate == "2018-04-22T19:56:00.963Z" select t;

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
           
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            var listToDoNetwork = await networkStore.FindAsync();
            var listToDoCache = await syncStore.FindAsync();

            var existingItemInNetwork1 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInNetwork2 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInNetwork3 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem3.ID);
            var existingItemInCache1 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInCache2 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInCache3 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem3.ID);

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);
            await autoStore.RemoveAsync(savedItem2.ID);

            // Assert            
            Assert.IsNull(existingItemInCache1);
            Assert.IsNotNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNotNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }


        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesWithLogicalAndExpressionsAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var networkStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task Delete",
                Details = "Details for",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Not Delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = autoStore.Where(x => x.Details.StartsWith("Details f") && x.DueDate.Equals("2018-04-22T19:56:00.963Z")).Where(y => y.Name.StartsWith("Task Del"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
           
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            var listToDoNetwork = await networkStore.FindAsync();
            var listToDoCache = await syncStore.FindAsync();

            var existingItemInNetwork1 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInNetwork2 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInNetwork3 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem3.ID);
            var existingItemInCache1 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInCache2 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInCache3 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem3.ID);

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);
            await autoStore.RemoveAsync(savedItem2.ID);

            // Assert            
            Assert.IsNull(existingItemInCache1);
            Assert.IsNotNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNotNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryMultipleWhereClausesWithLogicalOrExpressionsAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var networkStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.NETWORK);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Task Del1",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Not Del",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-21T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Del2",
                Details = "Details",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = autoStore.Where(x => x.Details.StartsWith("Details f") || x.DueDate.Equals("2018-04-22T19:56:00.963Z")).Where(y => y.Name.StartsWith("Task D"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
          
            // Act
            var kinveyDeleteResponse = await autoStore.RemoveAsync(query);

            var listToDoNetwork = await networkStore.FindAsync();
            var listToDoCache = await syncStore.FindAsync();

            var existingItemInNetwork1 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInNetwork2 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInNetwork3 = listToDoNetwork.FirstOrDefault(todo => todo.ID == savedItem3.ID);
            var existingItemInCache1 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem1.ID);
            var existingItemInCache2 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem2.ID);
            var existingItemInCache3 = listToDoCache.FirstOrDefault(todo => todo.ID == savedItem3.ID);

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);
            await autoStore.RemoveAsync(savedItem2.ID);

            // Assert            
            Assert.IsNull(existingItemInCache1);
            Assert.IsNotNull(existingItemInCache2);
            Assert.IsNull(existingItemInCache3);
            Assert.IsNotNull(kinveyDeleteResponse);
            Assert.AreEqual(1, kinveyDeleteResponse.count);
            Assert.IsNotNull(existingItemInNetwork1);
            Assert.IsNotNull(existingItemInNetwork2);
            Assert.IsNull(existingItemInNetwork3);
        }

        [TestMethod]
        public async Task TestDeleteByQueryWhereClauseIsAbsentInQueryUsingSelectClauseAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(7);
            }
           
            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = todoStore.Select(x => x.Details);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);
            
            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            var e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });

            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_GENERAL);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_DATASTORE_WHERE_CLAUSE_IS_ABSENT_IN_QUERY);
        }

        [TestMethod]
        public async Task TestDeleteByQueryWhereClauseIsAbsentInQueryUsingOrderClauseAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(7);
            }
            
            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = todoStore.OrderBy(x => x.Details);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);
            
            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            var e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });

            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_GENERAL);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_DATASTORE_WHERE_CLAUSE_IS_ABSENT_IN_QUERY);
        }

        [TestMethod]
        public async Task TestDeleteByQueryWhereClauseIsAbsentInQueryUsingTakeClauseAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(7);
            }
            
            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = todoStore.Take(1);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);
         
            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            Exception e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });

            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_GENERAL);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_DATASTORE_WHERE_CLAUSE_IS_ABSENT_IN_QUERY);
        }

        [TestMethod]
        public async Task TestDeleteByQueryNullQueryAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(7);
            }
            
            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 not to delete",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            var e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query: null);
            });

            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_GENERAL);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_DATASTORE_NULL_QUERY);
        }

        [TestMethod]
        public async Task TestDeleteByQueryNotSupportedExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(7);
            }
            
            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task Del11",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task Del22",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);

            var query = todoStore.Where(x => true);

            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            var e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });

            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_DATASTORE_NETWORK);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_LINQ_WHERE_CLAUSE_NOT_SUPPORTED);
        }

        [TestMethod]
        public async Task TestDeleteByQueryNotSupportedStringExpressionAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(7);
            }
            
            // Arrange
            var todoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);

            var newItem1 = new ToDo
            {
                Name = "Task1 not to delete",
                Details = "Details1",
                DueDate = "2017-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 18, 19, 56, 0),
                BoolVal = false,
                Value = 3
            };
            var newItem2 = new ToDo
            {
                Name = "Task2 to delete support",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                NewDate = new DateTime(2018, 12, 10, 0, 0, 0),

            };
            var newItem3 = new ToDo
            {
                Name = "Task3 to delete support",
                Details = "Details for",
                BoolVal = true,
                DueDate = "2018-04-22T19:56:00.963Z",
                Value = 1
            };

            var query = todoStore.Where(x => x.Name.Contains("support"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await todoStore.SaveAsync(newItem1);
            var savedItem2 = await todoStore.SaveAsync(newItem2);
            var savedItem3 = await todoStore.SaveAsync(newItem3);
           
            KinveyDeleteResponse kinveyDeleteResponse;

            // Act
            var e = await Assert.ThrowsExceptionAsync<KinveyException>(async delegate
            {
                kinveyDeleteResponse = await todoStore.RemoveAsync(query);
            });

            // Teardown
            await todoStore.RemoveAsync(savedItem1.ID);
            await todoStore.RemoveAsync(savedItem2.ID);
            await todoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsTrue(e.GetType() == typeof(KinveyException));
            KinveyException ke = e as KinveyException;
            Assert.IsTrue(ke.ErrorCategory == EnumErrorCategory.ERROR_DATASTORE_NETWORK);
            Assert.IsTrue(ke.ErrorCode == EnumErrorCode.ERROR_LINQ_WHERE_CLAUSE_NOT_SUPPORTED);
        }

        #endregion Remove by query

        #region Remove by id

        [TestMethod]
        public async Task TestRemoveByIdAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(3, kinveyClient);
            }

            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);

            var newItem = new ToDo
            {
                Name = "todo save",
                Details = "details for save",
                DueDate = "2016-04-22T19:56:00.961Z"
            };
           
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem = await autoStore.SaveAsync(newItem);

            // Act
            var kdr = await autoStore.RemoveAsync(savedItem.ID);

            // Assert
            Assert.IsNotNull(kdr);
            Assert.AreEqual(1, kdr.count);
        }

        #endregion Remove by id

        #region Find

        [TestMethod]
        public async Task TestFindByQueryNetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(6, kinveyClient);
            }

            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);

            var newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1 task",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem2 = new ToDo
            {
                Name = "another todo",
                Details = "details for 2 task",
                DueDate = "2016-04-22T19:56:00.963Z"
            };
           
            var query = autoStore.Where(x => x.Details.StartsWith("details for 1"));
           
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            newItem1 = await autoStore.SaveAsync(newItem1);
            newItem2 = await autoStore.SaveAsync(newItem2);
           
            // Act          
            var listToDoNetwork = await autoStore.FindAsync(query);

            // Teardown
            await autoStore.RemoveAsync(newItem1.ID);
            await autoStore.RemoveAsync(newItem2.ID);

            // Assert
            Assert.IsNotNull(listToDoNetwork);
            Assert.AreEqual(1, listToDoNetwork.Count);
            Assert.AreEqual(newItem1.Name, listToDoNetwork.First().Name);
            Assert.AreEqual(newItem1.Details, listToDoNetwork.First().Details);
            Assert.AreEqual(newItem1.DueDate, listToDoNetwork.First().DueDate);
        }

        [TestMethod]
        public async Task TestFindByQueryNetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }

            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1 task",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem2 = new ToDo
            {
                Name = "another todo",
                Details = "details for 2 task",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var query = autoStore.Where(x => x.Details.StartsWith("details for 1"));
           
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            SetUnReachableRootUrlToKinveyClient();

            newItem1 = await syncStore.SaveAsync(newItem1);
            newItem2 = await syncStore.SaveAsync(newItem2);
           
            // Act          
            var listToDoCache = await autoStore.FindAsync(query);

            // Teardown
            await syncStore.RemoveAsync(newItem1.ID);
            await syncStore.RemoveAsync(newItem2.ID);

            // Assert
            Assert.IsNotNull(listToDoCache);
            Assert.AreEqual(1, listToDoCache.Count);
            Assert.AreEqual(newItem1.Name, listToDoCache.First().Name);
            Assert.AreEqual(newItem1.Details, listToDoCache.First().Details);
            Assert.AreEqual(newItem1.DueDate, listToDoCache.First().DueDate);
        }

        [TestMethod]
        public async Task TestFindByQueryTake1NetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(6, kinveyClient);
            }
          
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);

            var newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem2 = new ToDo
            {
                Name = "details for 2",
                Details = "details for 2",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var query = autoStore.Where(x => x.Details.StartsWith("det")).Take(1);
         
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            newItem1 = await autoStore.SaveAsync(newItem1);
            newItem2 = await autoStore.SaveAsync(newItem2);

            //Act
            var listToDoNetwork = await autoStore.FindAsync(query);

            // Teardown
            await autoStore.RemoveAsync(newItem1.ID);
            await autoStore.RemoveAsync(newItem2.ID);

            // Assert
            Assert.IsNotNull(listToDoNetwork);
            Assert.AreEqual(1, listToDoNetwork.Count);
        }

        [TestMethod]
        public async Task TestFindByQueryTake1NetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }
                    
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem2 = new ToDo
            {
                Name = "details for 2",
                Details = "details for 2",
                DueDate = "2016-04-22T19:56:00.963Z"
            };
          
            var query = autoStore.Where(x => x.Details.StartsWith("det")).Take(1);

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            SetUnReachableRootUrlToKinveyClient();

            newItem1 = await syncStore.SaveAsync(newItem1);
            newItem2 = await syncStore.SaveAsync(newItem2);
           
            //Act
            var listToDoCache = await autoStore.FindAsync(query);

            // Teardown
            await syncStore.RemoveAsync(newItem1.ID);
            await syncStore.RemoveAsync(newItem2.ID);

            // Assert
            Assert.IsNotNull(listToDoCache);
            Assert.AreEqual(1, listToDoCache.Count);
        }

        [TestMethod]
        public async Task TestFindByQuerySkip1NetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(6, kinveyClient);
            }
           
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);

            var newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem2 = new ToDo
            {
                Name = "details for 2",
                Details = "details for 2",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var query = autoStore.Where(x => x.Details.StartsWith("det")).Skip(1);

            await User.LoginAsync(TestSetup.user, TestSetup.pass);

            newItem1 = await autoStore.SaveAsync(newItem1);
            newItem2 = await autoStore.SaveAsync(newItem2);
            
            //Act
            var listToDoNetwork = await autoStore.FindAsync(query);

            // Teardown
            await autoStore.RemoveAsync(newItem1.ID);
            await autoStore.RemoveAsync(newItem2.ID);

            // Assert
            Assert.IsNotNull(listToDoNetwork);
            Assert.AreEqual(1, listToDoNetwork.Count);
        }

        [TestMethod]
        public async Task TestFindByQuerySkip1NetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }
          
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem2 = new ToDo
            {
                Name = "details for 2",
                Details = "details for 2",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var query = autoStore.Where(x => x.Details.StartsWith("det")).Skip(1);
           
            await User.LoginAsync(TestSetup.user, TestSetup.pass);

            SetUnReachableRootUrlToKinveyClient();

            newItem1 = await syncStore.SaveAsync(newItem1);
            newItem2 = await syncStore.SaveAsync(newItem2);
           
            //Act
            var listToDoCache = await autoStore.FindAsync(query);

            // Teardown
            await syncStore.RemoveAsync(newItem1.ID);
            await syncStore.RemoveAsync(newItem2.ID);

            // Assert
            Assert.IsNotNull(listToDoCache);
            Assert.AreEqual(1, listToDoCache.Count);
        }

        [TestMethod]
        public async Task TestFindByQuerySkip1Take1NetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8, kinveyClient);
            }
           
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);

            var newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem2 = new ToDo
            {
                Name = "another todo",
                Details = "details for 2",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem3 = new ToDo
            {
                Name = "yet another todo",
                Details = "details for 3",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var query = autoStore.Where(x => x.Details.StartsWith("det")).Skip(1).Take(1);

            await User.LoginAsync(TestSetup.user, TestSetup.pass);

            newItem1 = await autoStore.SaveAsync(newItem1);
            newItem2 = await autoStore.SaveAsync(newItem2);
            newItem3 = await autoStore.SaveAsync(newItem3);
            
            // Act
            var listToDoNetwork = await autoStore.FindAsync(query);

            // Teardown
            await autoStore.RemoveAsync(newItem1.ID);
            await autoStore.RemoveAsync(newItem2.ID);
            await autoStore.RemoveAsync(newItem3.ID);

            // Assert
            Assert.IsNotNull(listToDoNetwork);
            Assert.AreEqual(listToDoNetwork.Count, 1);
        }

        [TestMethod]
        public async Task TestFindByQuerySkip1Take1NetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }
                     
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "todo",
                Details = "details for 1",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem2 = new ToDo
            {
                Name = "another todo",
                Details = "details for 2",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var newItem3 = new ToDo
            {
                Name = "yet another todo",
                Details = "details for 3",
                DueDate = "2016-04-22T19:56:00.963Z"
            };

            var query = syncStore.Where(x => x.Details.StartsWith("det")).Skip(1).Take(1);
           
            await User.LoginAsync(TestSetup.user, TestSetup.pass);

            SetUnReachableRootUrlToKinveyClient();

            newItem1 = await syncStore.SaveAsync(newItem1);
            newItem2 = await syncStore.SaveAsync(newItem2);
            newItem3 = await syncStore.SaveAsync(newItem3);
            
            // Act
            var listToDoCache = await autoStore.FindAsync(query);

            // Teardown
            await syncStore.RemoveAsync(newItem1.ID);
            await syncStore.RemoveAsync(newItem2.ID);
            await syncStore.RemoveAsync(newItem3.ID);

            // Assert
            Assert.IsNotNull(listToDoCache);
            Assert.AreEqual(listToDoCache.Count, 1);
        }

        #endregion Find

        #region Find by id

        [TestMethod]
        public async Task TestFindByIDNetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(4, kinveyClient);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);

            var newItem = new ToDo
            {
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem = await autoStore.SaveAsync(newItem);

            // Act
            var networkEntity = await autoStore.FindByIDAsync(savedItem.ID);

            // Teardown
            await autoStore.RemoveAsync(savedItem.ID);

            // Assert
            Assert.IsNotNull(networkEntity);
            Assert.AreEqual(savedItem.ID, networkEntity.ID);
            Assert.AreEqual(savedItem.Name, networkEntity.Name);
            Assert.AreEqual(savedItem.Details, networkEntity.Details);
            Assert.AreEqual(savedItem.DueDate, networkEntity.DueDate);
        }

        [TestMethod]
        public async Task TestFindByIDNetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }

            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem = new ToDo
            {
                Name = "Next Task",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };
            
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            SetUnReachableRootUrlToKinveyClient();

            var savedItem = await syncStore.SaveAsync(newItem);

            // Act
            var cacheEntity = await autoStore.FindByIDAsync(savedItem.ID);

            // Teardown
            await syncStore.RemoveAsync(savedItem.ID);

            // Assert
            Assert.IsNotNull(cacheEntity);
            Assert.AreEqual(savedItem.ID, cacheEntity.ID);
            Assert.AreEqual(savedItem.Name, cacheEntity.Name);
            Assert.AreEqual(savedItem.Details, cacheEntity.Details);
            Assert.AreEqual(savedItem.DueDate, cacheEntity.DueDate);
        }

        #endregion Find by id

        #region Group and aggregate

        [TestMethod]
        public async Task TestGetSumNetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8, kinveyClient);
            }

            // Arrange           
            var autoStore = DataStore<Person>.Collection(personCollection, DataStoreType.AUTO);

            var p1 = new Person
            {
                FirstName = "Michael",
                LastName = "Bluth",
                Age = 40
            };           
            var p2 = new Person
            {
                FirstName = "George Michael",
                LastName = "Bluth",
                Age = 15
            };            
            var p3 = new Person
            {
                FirstName = "Tobias",
                LastName = "Funke",
                Age = 46
            };

            var query = autoStore.Where(x => x.LastName.Equals("Bluth"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            p1 = await autoStore.SaveAsync(p1);
            p2 = await autoStore.SaveAsync(p2);
            p3 = await autoStore.SaveAsync(p3);
            
            // Act
            var networkResult = await autoStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_SUM, "", "Age", query);

            // Teardown
            await autoStore.RemoveAsync(p3.ID);
            await autoStore.RemoveAsync(p2.ID);
            await autoStore.RemoveAsync(p1.ID);

            // Assert
            Assert.IsNotNull(networkResult);
            Assert.AreEqual(p1.Age + p2.Age, networkResult.First().Result);
        }

        [TestMethod]
        public async Task TestGetSumNetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }

            // Arrange           
            var autoStore = DataStore<Person>.Collection(personCollection, DataStoreType.AUTO);
            var syncStore = DataStore<Person>.Collection(personCollection, DataStoreType.SYNC);

            var p1 = new Person
            {
                FirstName = "Michael",
                LastName = "Bluth",
                Age = 40
            };           
            var p2 = new Person
            {
                FirstName = "George Michael",
                LastName = "Bluth",
                Age = 15
            };            
            var p3 = new Person
            {
                FirstName = "Tobias",
                LastName = "Funke",
                Age = 46
            };
            
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            SetUnReachableRootUrlToKinveyClient();

            p1 = await syncStore.SaveAsync(p1);
            p2 = await syncStore.SaveAsync(p2);
            p3 = await syncStore.SaveAsync(p3);

            var query = autoStore.Where(x => x.LastName.Equals("Bluth"));

            // Act
            var cacheResult = await autoStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_SUM, "", "Age", query);

            // Teardown
            await syncStore.RemoveAsync(p3.ID);
            await syncStore.RemoveAsync(p2.ID);
            await syncStore.RemoveAsync(p1.ID);

            // Assert
            Assert.IsNotNull(cacheResult);
            Assert.AreEqual(p1.Age + p2.Age, cacheResult.First().Result);
        }

        [TestMethod]
        public async Task TestGetMinNetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8, kinveyClient);
            }

            // Arrange            
            var autoStore = DataStore<Person>.Collection(personCollection, DataStoreType.AUTO);

            var p1 = new Person
            {
                FirstName = "Michael",
                LastName = "Bluth",
                Age = 40
            };           
            var p2 = new Person
            {
                FirstName = "George Michael",
                LastName = "Bluth",
                Age = 15
            };
            var p3 = new Person
            {
                FirstName = "Tobias",
                LastName = "Funke",
                Age = 46
            };
            
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            p1 = await autoStore.SaveAsync(p1);
            p2 = await autoStore.SaveAsync(p2);
            p3 = await autoStore.SaveAsync(p3);

            // Act
            var networkResult = await autoStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_MIN, "", "Age", null);

            // Teardown
            await autoStore.RemoveAsync(p3.ID);
            await autoStore.RemoveAsync(p2.ID);
            await autoStore.RemoveAsync(p1.ID);

            // Assert
            Assert.IsNotNull(networkResult);
            Assert.AreEqual(p2.Age, networkResult.First().Result);
        }

        [TestMethod]
        public async Task TestGetMinNetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }

            // Arrange          
            var autoStore = DataStore<Person>.Collection(personCollection, DataStoreType.AUTO);
            var syncStore = DataStore<Person>.Collection(personCollection, DataStoreType.SYNC);

            var p1 = new Person
            {
                FirstName = "Michael",
                LastName = "Bluth",
                Age = 40
            };           
            var p2 = new Person
            {
                FirstName = "George Michael",
                LastName = "Bluth",
                Age = 15
            };            
            var p3 = new Person
            {
                FirstName = "Tobias",
                LastName = "Funke",
                Age = 46
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            SetUnReachableRootUrlToKinveyClient();

            p1 = await syncStore.SaveAsync(p1);
            p2 = await syncStore.SaveAsync(p2);
            p3 = await syncStore.SaveAsync(p3);

            // Act
            var cacheResult = await autoStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_MIN, "", "Age", null);

            // Teardown
            await syncStore.RemoveAsync(p3.ID);
            await syncStore.RemoveAsync(p2.ID);
            await syncStore.RemoveAsync(p1.ID);

            // Assert
            Assert.IsNotNull(cacheResult);
            Assert.AreEqual(p2.Age, cacheResult.First().Result);
        }

        [TestMethod]
        public async Task TestGetMaxNetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8, kinveyClient);
            }

            // Arrange           
            var autoStore = DataStore<Person>.Collection(personCollection, DataStoreType.AUTO);

            var p1 = new Person
            {
                FirstName = "Michael",
                LastName = "Bluth",
                Age = 40
            };            
            var p2 = new Person
            {
                FirstName = "George Michael",
                LastName = "Bluth",
                Age = 15
            };           
            var p3 = new Person
            {
                FirstName = "Tobias",
                LastName = "Funke",
                Age = 46
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            p1 = await autoStore.SaveAsync(p1);
            p2 = await autoStore.SaveAsync(p2);
            p3 = await autoStore.SaveAsync(p3);

            // Act
            var networkResult = await autoStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_MAX, "", "Age", null);

            // Teardown
            await autoStore.RemoveAsync(p3.ID);
            await autoStore.RemoveAsync(p2.ID);
            await autoStore.RemoveAsync(p1.ID);

            // Assert
            Assert.IsNotNull(networkResult);
            Assert.AreEqual(p3.Age, networkResult.First().Result);
        }

        [TestMethod]
        public async Task TestGetMaxNetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }

            // Arrange                       
            var autoStore = DataStore<Person>.Collection(personCollection, DataStoreType.AUTO);
            var syncStore = DataStore<Person>.Collection(personCollection, DataStoreType.SYNC);

            var p1 = new Person
            {
                FirstName = "Michael",
                LastName = "Bluth",
                Age = 40
            };            
            var p2 = new Person
            {
                FirstName = "George Michael",
                LastName = "Bluth",
                Age = 15
            };            
            var p3 = new Person
            {
                FirstName = "Tobias",
                LastName = "Funke",
                Age = 46
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            SetUnReachableRootUrlToKinveyClient();

            p1 = await syncStore.SaveAsync(p1);
            p2 = await syncStore.SaveAsync(p2);
            p3 = await syncStore.SaveAsync(p3);

            // Act
            var cacheResult = await autoStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_MAX, "", "Age", null);

            // Teardown
            await syncStore.RemoveAsync(p3.ID);
            await syncStore.RemoveAsync(p2.ID);
            await syncStore.RemoveAsync(p1.ID);

            // Assert
            Assert.IsNotNull(cacheResult);
            Assert.AreEqual(p3.Age, cacheResult.First().Result);
        }

        [TestMethod]
        public async Task TestGetAverageNetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(10, kinveyClient);
            }

            // Arrange            
            var autoStore = DataStore<Person>.Collection(personCollection, DataStoreType.AUTO);

            var p1 = new Person
            {
                FirstName = "Michael",
                LastName = "Bluth",
                Age = 40
            };
            var p2 = new Person
            {
                FirstName = "George Michael",
                LastName = "Bluth",
                Age = 15
            };
            var p3 = new Person
            {
                FirstName = "Tobias",
                LastName = "Funke",
                Age = 46
            };            
            var p4 = new Person
            {
                FirstName = "Buster",
                LastName = "Bluth",
                Age = 19
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            p1 = await autoStore.SaveAsync(p1);
            p2 = await autoStore.SaveAsync(p2);
            p3 = await autoStore.SaveAsync(p3);
            p4 = await autoStore.SaveAsync(p4);

            // Act
            var networkResult = await autoStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_AVERAGE, "", "Age", null);

            // Teardown
            await autoStore.RemoveAsync(p4.ID);
            await autoStore.RemoveAsync(p3.ID);
            await autoStore.RemoveAsync(p2.ID);
            await autoStore.RemoveAsync(p1.ID);

            // Assert
            Assert.IsNotNull(networkResult);
            Assert.AreEqual((p1.Age + p2.Age + p3.Age + p4.Age) / 4, networkResult.First().Result);
        }

        [TestMethod]
        public async Task TestGetAverageNetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }

            // Arrange                      
            var autoStore = DataStore<Person>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<Person>.Collection(toDosCollection, DataStoreType.SYNC);

            var p1 = new Person
            {
                FirstName = "Michael",
                LastName = "Bluth",
                Age = 40
            };           
            var p2 = new Person
            {
                FirstName = "George Michael",
                LastName = "Bluth",
                Age = 15
            };            
            var p3 = new Person
            {
                FirstName = "Tobias",
                LastName = "Funke",
                Age = 46
            };            
            var p4 = new Person
            {
                FirstName = "Buster",
                LastName = "Bluth",
                Age = 19
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            SetUnReachableRootUrlToKinveyClient();

            p1 = await syncStore.SaveAsync(p1);
            p2 = await syncStore.SaveAsync(p2);
            p3 = await syncStore.SaveAsync(p3);
            p4 = await syncStore.SaveAsync(p4);

            // Act
            var cacheResult = await autoStore.GroupAndAggregateAsync(EnumReduceFunction.REDUCE_FUNCTION_AVERAGE, "", "Age", null);

            // Teardown
            await syncStore.RemoveAsync(p4.ID);
            await syncStore.RemoveAsync(p3.ID);
            await syncStore.RemoveAsync(p2.ID);
            await syncStore.RemoveAsync(p1.ID);

            // Assert
            Assert.IsNotNull(cacheResult);
            Assert.AreEqual((p1.Age + p2.Age + p3.Age + p4.Age) / 4, cacheResult.First().Result);
        }

        #endregion Group and aggregate  

        #region Save

        [TestMethod]
        public async Task TestSaveAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(4, kinveyClient);
            }

            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem = new ToDo
            {
                Name = "todo save",
                Details = "details for save",
                DueDate = "2016-04-22T19:56:00.961Z"
            };
           
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            var savedItem = await autoStore.SaveAsync(newItem);

            var existingItemNetwork = await autoStore.FindByIDAsync(savedItem.ID);
            var existingItemCache = await syncStore.FindByIDAsync(savedItem.ID);

            //Teardown
            await autoStore.RemoveAsync(savedItem.ID);

            // Assert
            Assert.IsNotNull(savedItem);
            Assert.IsNotNull(existingItemNetwork);
            Assert.IsNotNull(existingItemCache);
            Assert.AreEqual(savedItem.ID, existingItemNetwork.ID);
            Assert.AreEqual(savedItem.Details, existingItemNetwork.Details);
            Assert.AreEqual(savedItem.DueDate, existingItemNetwork.DueDate);
            Assert.AreEqual(savedItem.ID, existingItemCache.ID);
            Assert.AreEqual(savedItem.Details, existingItemCache.Details);
            Assert.AreEqual(savedItem.DueDate, existingItemCache.DueDate);
        }

        #endregion Save

        #region Get count

        [TestMethod]
        public async Task TestGetCountNetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8, kinveyClient);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);

            var newItem1 = new ToDo
            {
                Name = "Next Task1",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var newItem2 = new ToDo
            {
                Name = "Next Task2",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var newItem3 = new ToDo
            {
                Name = "Next Task3",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            ToDo savedItem1 = await autoStore.SaveAsync(newItem1);
            ToDo savedItem2 = await autoStore.SaveAsync(newItem2);
            ToDo savedItem3 = await autoStore.SaveAsync(newItem3);

            // Act
            var networkResult = await autoStore.GetCountAsync();

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);
            await autoStore.RemoveAsync(savedItem2.ID);
            await autoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsNotNull(networkResult);
            Assert.AreEqual(3u, networkResult);
        }

        [TestMethod]
        public async Task TestGetCountNetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }
                        
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Next Task1",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var newItem2 = new ToDo
            {
                Name = "Next Task2",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var newItem3 = new ToDo
            {
                Name = "Next Task3",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };
                      
            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            SetUnReachableRootUrlToKinveyClient();

            var savedItem1 = await syncStore.SaveAsync(newItem1);
            var savedItem2 = await syncStore.SaveAsync(newItem2);
            var savedItem3 = await syncStore.SaveAsync(newItem3);

            // Act
            var networkResult = await autoStore.GetCountAsync();

            // Teardown
            await syncStore.RemoveAsync(savedItem1.ID);
            await syncStore.RemoveAsync(savedItem2.ID);
            await syncStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsNotNull(networkResult);
            Assert.AreEqual(3u, networkResult);
        }

        [TestMethod]
        public async Task TestGetCountWithQueryNetworkConnectionAvailableAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(8, kinveyClient);
            }
            
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);

            var newItem1 = new ToDo
            {
                Name = "Next Task1",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var newItem2 = new ToDo
            {
                Name = "Next Task2",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var newItem3 = new ToDo
            {
                Name = "Next Task3",
                Details = "Details",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var query = autoStore.Where(t => t.Details.StartsWith("A t"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            var savedItem1 = await autoStore.SaveAsync(newItem1);
            var savedItem2 = await autoStore.SaveAsync(newItem2);
            var savedItem3 = await autoStore.SaveAsync(newItem3);
            
            // Act
            var networkResult = await autoStore.GetCountAsync(query);

            // Teardown
            await autoStore.RemoveAsync(savedItem1.ID);
            await autoStore.RemoveAsync(savedItem2.ID);
            await autoStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsNotNull(networkResult);
            Assert.AreEqual(2u, networkResult);
        }

        [TestMethod]
        public async Task TestGetCountWithQueryNetworkConnectionIssueAsync()
        {
            // Setup
            if (MockData)
            {
                MockResponses(1, kinveyClient);
            }
           
            // Arrange
            var autoStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.AUTO);
            var syncStore = DataStore<ToDo>.Collection(toDosCollection, DataStoreType.SYNC);

            var newItem1 = new ToDo
            {
                Name = "Next Task1",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var newItem2 = new ToDo
            {
                Name = "Next Task2",
                Details = "A test",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var newItem3 = new ToDo
            {
                Name = "Next Task3",
                Details = "Details",
                DueDate = "2016-04-19T20:02:17.635Z"
            };

            var query = autoStore.Where(t => t.Details.StartsWith("A t"));

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            SetUnReachableRootUrlToKinveyClient();

            var savedItem1 = await syncStore.SaveAsync(newItem1);
            var savedItem2 = await syncStore.SaveAsync(newItem2);
            var savedItem3 = await syncStore.SaveAsync(newItem3);
            
            // Act
            var networkResult = await autoStore.GetCountAsync(query);

            // Teardown
            await syncStore.RemoveAsync(savedItem1.ID);
            await syncStore.RemoveAsync(savedItem2.ID);
            await syncStore.RemoveAsync(savedItem3.ID);

            // Assert
            Assert.IsNotNull(networkResult);
            Assert.AreEqual(2u, networkResult);
        }

        #endregion Get count

        #region Delta set

        #region Pull

        [TestMethod]
        public async Task TestDeltaSetPullNoChanges()
        {
            // Setup
            if (MockData)
            {
                MockResponses(6, kinveyClient);
            }

            //Arrange
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            fc1 = await store.SaveAsync(fc1);

            // Act
            var firstResponse = await store.PullAsync();
            var secondResponse = await store.PullAsync();

            //Teardown
            var existingEntities = await store.FindAsync();
            if (existingEntities != null)
            {
                await store.RemoveAsync(existingEntities.First().ID);
            }

            // Assert
            Assert.AreEqual(1, firstResponse.PullCount);
            Assert.AreEqual(0, secondResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetPullReturnOnlyChanges()
        {
            // Setup
            if (MockData)
            {
                MockResponses(11, kinveyClient);
            }

            //Arrange
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await store.SaveAsync(fc1);
            var firstResponse = await store.PullAsync();

            fc2 = await store.SaveAsync(fc2);
            var secondResponse = await store.PullAsync();

            fc3 = await store.SaveAsync(fc3);
            var thirdResponse = await store.PullAsync();

            //Teardown
            var existingEntities = await store.FindAsync();
            if (existingEntities != null)
            {
                foreach (var existingEntity in existingEntities)
                {
                    await store.RemoveAsync(existingEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(1, firstResponse.PullCount);
            Assert.AreEqual(1, secondResponse.PullCount);
            Assert.AreEqual(1, thirdResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetPullReturnOnlyUpdates()
        {
            // Setup
            if (MockData)
            {
                MockResponses(12, kinveyClient);
            }

            //Arrange
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await store.SaveAsync(fc1);
            fc2 = await store.SaveAsync(fc2);
            fc3 = await store.SaveAsync(fc3);
            var query = store.Where(x => x.Question.StartsWith("Wh"));
            var firstResponse = await store.PullAsync(query);

            var fc2Query = store.Where(y => y.Answer.Equals("8"));
            fc2 = (await store.FindAsync(fc2Query)).First();
            fc2.Answer = "14";
            fc2 = await networkStore.SaveAsync(fc2);
            var query2 = store.Where(x => x.Question.StartsWith("Wh"));
            var secondResponse = await store.PullAsync(query2);

            //Teardown
            var existingEntities = await store.FindAsync();
            if (existingEntities != null)
            {
                foreach (var existingEntity in existingEntities)
                {
                    await store.RemoveAsync(existingEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullCount);
            Assert.AreEqual(1, secondResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetPullReturnOnlyDeletes()
        {
            // Setup
            if (MockData)
            {
                MockResponses(11, kinveyClient);
            }
           
            //Arrange
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await store.SaveAsync(fc1);
            fc2 = await store.SaveAsync(fc2);
            fc3 = await store.SaveAsync(fc3);
            var query = store.Where(x => x.Question.StartsWith("Wh"));
            var firstResponse = await store.PullAsync(query);

            var fc2Query = store.Where(y => y.Answer.Equals("8"));
            fc2 = (await store.FindAsync(fc2Query)).First();
            int networkDeleteCount = (await networkStore.RemoveAsync(fc2.ID)).count;
            var query2 = store.Where(x => x.Question.StartsWith("Wh"));
            var secondResponse = await store.PullAsync(query2);

            var networkEntities = await store.FindAsync();
            int networkCount = networkEntities.Count;

            //Teardown
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullCount);
            Assert.AreEqual(0, secondResponse.PullCount);
            Assert.AreEqual(2, networkCount);
            Assert.AreEqual(1, networkDeleteCount);
        }

        [TestMethod]
        public async Task TestDeltaSetPullReturnCorrectNumberOfCreatedItems()
        {
            // Setup
            if (MockData)
            {
                MockResponses(10, kinveyClient);
            }
            
            //Arrange
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await store.SaveAsync(fc1);
            var firstResponse = await store.PullAsync();

            fc2 = await store.SaveAsync(fc2);
            fc3 = await store.SaveAsync(fc3);
            var secondResponse = await store.PullAsync();

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(1, firstResponse.PullCount);
            Assert.AreEqual(2, secondResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetPullReturnCorrectNumberOfUpdates()
        {
            // Setup
            if (MockData)
            {
                MockResponses(15, kinveyClient);
            }
           
            //Arrange
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await networkStore.SaveAsync(fc1);
            fc2 = await networkStore.SaveAsync(fc2);
            fc3 = await networkStore.SaveAsync(fc3);
            var query = store.Where(x => x.Question.StartsWith("Wh"));
            var firstResponse = await store.PullAsync(query);

            var fc2Query = store.Where(y => y.Answer.Equals("8"));
            fc2 = (await store.FindAsync(fc2Query)).First();
            fc2.Answer = "14";
            fc2 = await networkStore.SaveAsync(fc2);
            var query2 = store.Where(x => x.Question.StartsWith("Wh"));
            var secondResponse = await store.PullAsync(query2);

            fc1.Answer = "15";
            fc1 = await networkStore.SaveAsync(fc1);

            fc2.Answer = "16";
            fc2 = await networkStore.SaveAsync(fc2);
            var query3 = store.Where(x => x.Question.StartsWith("Wh"));
            var thirdResponse = await store.PullAsync(query3);

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullCount);
            Assert.AreEqual(1, secondResponse.PullCount);
            Assert.AreEqual(2, thirdResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetWithQueryPullReturnCorrectNumberOfUpdates()
        {
            // Setup
            if (MockData)
            {
                MockResponses(12, kinveyClient);
            }

            // Arrange           
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What+?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What+?",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "What+?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await networkStore.SaveAsync(fc1);
            fc2 = await networkStore.SaveAsync(fc2);
            fc3 = await networkStore.SaveAsync(fc3);
            var query = store.Where(x => x.Question.Equals("What+?"));
            var firstResponse = await store.PullAsync(query);

            var fc2Query = store.Where(y => y.Answer.Equals("8"));
            fc2 = (await store.FindAsync(fc2Query)).First();
            fc2.Answer = "14";
            fc2 = await networkStore.SaveAsync(fc2);
            var query2 = store.Where(x => x.Question.Equals("What+?"));
            var secondResponse = await store.PullAsync(query2);

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullCount);
            Assert.AreEqual(1, secondResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetPullReturnCorrectNumberOfDeletes()
        {
            // Setup
            if (MockData)
            {
                MockResponses(11, kinveyClient);
            }
          
            //Arrange
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await networkStore.SaveAsync(fc1);
            fc2 = await networkStore.SaveAsync(fc2);
            fc3 = await networkStore.SaveAsync(fc3);
            var firstResponse = await store.PullAsync();

            var firstDeleteResponse = await store.RemoveAsync(fc1.ID);
            var secondResponse = await store.PullAsync();

            var secondDeleteResponse = await store.RemoveAsync(fc2.ID);
            var thirdDeleteResponse = await store.RemoveAsync(fc3.ID);
            var thirdResponse = await store.PullAsync();

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullCount);
            Assert.AreEqual(0, secondResponse.PullCount);
            Assert.AreEqual(0, thirdResponse.PullCount);
            Assert.AreEqual(1, firstDeleteResponse.count);
            Assert.AreEqual(1, secondDeleteResponse.count);
            Assert.AreEqual(1, thirdDeleteResponse.count);
        }

        [TestMethod]
        public async Task TestDeltaSetPullReturnCorrectNumberOfDeletesAndUpdates()
        {
            // Setup
            if (MockData)
            {
                MockResponses(12, kinveyClient);
            }

            //Arrange
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await networkStore.SaveAsync(fc1);
            fc2 = await networkStore.SaveAsync(fc2);
            fc3 = await networkStore.SaveAsync(fc3);
            var firstResponse = await store.PullAsync();

            var fc2Query = store.Where(y => y.Answer.Equals("8"));
            fc2 = (await store.FindAsync(fc2Query)).First();
            fc2.Answer = "14";
            fc2 = await networkStore.SaveAsync(fc2);
            var deleteResponse = await networkStore.RemoveAsync(fc3.ID);
            var secondResponse = await store.PullAsync();

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullCount);
            Assert.AreEqual(1, secondResponse.PullCount);
            Assert.AreEqual(1, deleteResponse.count);
        }

        #endregion Pull

        #region Sync

        [TestMethod]
        public async Task TestDeltaSetSyncNoChanges()
        {
            // Setup
            if (MockData)
            {
                MockResponses(6, kinveyClient);
            }
          
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            fc1 = await store.SaveAsync(fc1);

            // Act
            var firstResponse = await store.SyncAsync();
            var secondResponse = await store.SyncAsync();

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                await store.RemoveAsync(networkEntities.First().ID);
            }

            // Assert
            Assert.AreEqual(1, firstResponse.PullResponse.PullCount);
            Assert.AreEqual(0, secondResponse.PullResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetSyncReturnOnlyChanges()
        {
            // Setup
            if (MockData)
            {
                MockResponses(11, kinveyClient);
            }
           
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await store.SaveAsync(fc1);
            var firstResponse = await store.SyncAsync();

            fc2 = await store.SaveAsync(fc2);
            var secondResponse = await store.SyncAsync();

            fc3 = await store.SaveAsync(fc3);
            var thirdResponse = await store.SyncAsync();

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(1, firstResponse.PullResponse.PullCount);
            Assert.AreEqual(1, secondResponse.PullResponse.PullCount);
            Assert.AreEqual(1, thirdResponse.PullResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetSyncReturnOnlyUpdates()
        {
            // Setup
            if (MockData)
            {
                MockResponses(12, kinveyClient);
            }
            
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await store.SaveAsync(fc1);
            fc2 = await store.SaveAsync(fc2);
            fc3 = await store.SaveAsync(fc3);
            var query = store.Where(x => x.Question.StartsWith("Wh"));
            var firstResponse = await store.SyncAsync(query);

            var fc2Query = store.Where(y => y.Answer.Equals("8"));
            fc2 = (await store.FindAsync(fc2Query)).First();
            fc2.Answer = "14";
            fc2 = await networkStore.SaveAsync(fc2);
            var query2 = store.Where(x => x.Question.StartsWith("Wh"));
            var secondResponse = await store.SyncAsync(query2);

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullResponse.PullCount);
            Assert.AreEqual(1, secondResponse.PullResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetSyncReturnOnlyDeletes()
        {
            // Setup
            if (MockData)
            {
                MockResponses(11, kinveyClient);
            }
            
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await store.SaveAsync(fc1);
            fc2 = await store.SaveAsync(fc2);
            fc3 = await store.SaveAsync(fc3);
            var query = store.Where(x => x.Question.StartsWith("Wh"));
            var firstResponse = await store.SyncAsync(query);

            var fc2Query = store.Where(y => y.Answer.Equals("8"));
            fc2 = (await store.FindAsync(fc2Query)).First();
            int removedCount = (await networkStore.RemoveAsync(fc2.ID)).count;
            var query2 = store.Where(x => x.Question.StartsWith("Wh"));
            var secondResponse = await store.SyncAsync(query2);

            var networkEntities = await store.FindAsync();
            int networkCount = networkEntities.Count;

            //Teardown
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullResponse.PullCount);
            Assert.AreEqual(0, secondResponse.PullResponse.PullCount);
            Assert.AreEqual(2, networkCount);
            Assert.AreEqual(1, removedCount);
        }

        [TestMethod]
        public async Task TestDeltaSetSyncReturnCorrectNumberOfCreatedItems()
        {
            // Setup
            if (MockData)
            {
                MockResponses(10, kinveyClient);
            }
           
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await store.SaveAsync(fc1);
            var firstResponse = await store.SyncAsync();

            fc2 = await store.SaveAsync(fc2);
            fc3 = await store.SaveAsync(fc3);
            var secondResponse = await store.SyncAsync();

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(1, firstResponse.PullResponse.PullCount);
            Assert.AreEqual(2, secondResponse.PullResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetSyncReturnCorrectNumberOfUpdates()
        {
            // Setup
            if (MockData)
            {
                MockResponses(15, kinveyClient);
            }
           
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await networkStore.SaveAsync(fc1);
            fc2 = await networkStore.SaveAsync(fc2);
            fc3 = await networkStore.SaveAsync(fc3);
            var query = store.Where(x => x.Question.StartsWith("Wh"));
            var firstResponse = await store.SyncAsync(query);

            var fc2Query = store.Where(y => y.Answer.Equals("8"));
            fc2 = (await store.FindAsync(fc2Query)).First();
            fc2.Answer = "14";
            fc2 = await networkStore.SaveAsync(fc2);
            var query2 = store.Where(x => x.Question.StartsWith("Wh"));
            var secondResponse = await store.SyncAsync(query2);

            fc1.Answer = "15";
            fc1 = await networkStore.SaveAsync(fc1);

            fc2.Answer = "16";
            fc2 = await networkStore.SaveAsync(fc2);
            var query3 = store.Where(x => x.Question.StartsWith("Wh"));
            var thirdResponse = await store.SyncAsync(query3);

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullResponse.PullCount);
            Assert.AreEqual(1, secondResponse.PullResponse.PullCount);
            Assert.AreEqual(2, thirdResponse.PullResponse.PullCount);
        }

        [TestMethod]
        public async Task TestDeltaSetSyncReturnCorrectNumberOfDeletes()
        {
            // Setup
            if (MockData)
            {
                MockResponses(11, kinveyClient);
            }
           
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await networkStore.SaveAsync(fc1);
            fc2 = await networkStore.SaveAsync(fc2);
            fc3 = await networkStore.SaveAsync(fc3);
            var firstResponse = await store.SyncAsync();

            var firstDeleteResponse = await store.RemoveAsync(fc1.ID);
            var secondResponse = await store.SyncAsync();

            var secondDeleteResponse = await store.RemoveAsync(fc2.ID);
            var thirdDeleteResponse = await store.RemoveAsync(fc3.ID);
            var thirdResponse = await store.SyncAsync();

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullResponse.PullCount);
            Assert.AreEqual(0, secondResponse.PullResponse.PullCount);
            Assert.AreEqual(0, thirdResponse.PullResponse.PullCount);
            Assert.AreEqual(1, firstDeleteResponse.count);
            Assert.AreEqual(1, secondDeleteResponse.count);
            Assert.AreEqual(1, thirdDeleteResponse.count);
        }

        [TestMethod]
        public async Task TestDeltaSetSyncReturnCorrectNumberOfDeletesAndUpdates()
        {
            // Setup
            if (MockData)
            {
                MockResponses(12, kinveyClient);
            }
          
            var store = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.AUTO);
            var networkStore = DataStore<FlashCard>.Collection(flashCardCollection, DataStoreType.NETWORK);
            store.DeltaSetFetchingEnabled = true;

            var fc1 = new FlashCard
            {
                Question = "What is 2 + 5?",
                Answer = "7"
            };

            var fc2 = new FlashCard
            {
                Question = "What is 3 + 5",
                Answer = "8"
            };

            var fc3 = new FlashCard
            {
                Question = "Why is 6 afraid of 7?",
                Answer = "Because 7 8 9."
            };

            await User.LoginAsync(TestSetup.user, TestSetup.pass, kinveyClient);

            // Act
            fc1 = await networkStore.SaveAsync(fc1);
            fc2 = await networkStore.SaveAsync(fc2);
            fc3 = await networkStore.SaveAsync(fc3);
            var firstResponse = await store.SyncAsync();

            var fc2Query = store.Where(y => y.Answer.Equals("8"));
            fc2 = (await store.FindAsync(fc2Query)).First();
            fc2.Answer = "14";
            fc2 = await networkStore.SaveAsync(fc2);
            var deleteResponse = await networkStore.RemoveAsync(fc3.ID);
            var secondResponse = await store.SyncAsync();

            //Teardown
            var networkEntities = await store.FindAsync();
            if (networkEntities != null)
            {
                foreach (var networkEntity in networkEntities)
                {
                    await store.RemoveAsync(networkEntity.ID);
                }
            }

            // Assert
            Assert.AreEqual(3, firstResponse.PullResponse.PullCount);
            Assert.AreEqual(1, secondResponse.PullResponse.PullCount);
            Assert.AreEqual(1, deleteResponse.count);
        }

        #endregion Sync

        #endregion Delta set
    }
}
