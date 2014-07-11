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

namespace Kinvey.DotNet.Framework.Auth
{
    public class InMemoryCredentialStore : ICredentialStore
    {
        private Dictionary<string, Credential> store = new Dictionary<string, Credential>();

		public InMemoryCredentialStore(){
		}

        public Credential Load(string userId)
        {
            return store[userId];
        }

        public void Store(string userId, Credential credential)
        {
            Credential cred = new Credential(userId, credential.AuthToken);
            if (userId != null)
            {
                store.Add(userId, cred);
            }
        }

        public void Delete(string userId)
        {
            if (userId != null)
            {
                store.Remove(userId);
            }
        }
    }
}
