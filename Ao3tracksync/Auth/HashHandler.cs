/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Ao3tracksync.Auth
{
    public static class HashHandler
    {
        const int SALT_SIZE = 8;

        static System.Func<HashAlgorithm>[] Hashers = new System.Func<HashAlgorithm>[]
        {
            () => new SHA384Managed(),   // 0
        };

        static int CurrentAlgoIndex { get { return Hashers.Length - 1; } }
        static HashAlgorithm GetHashAlgo(int i) { if (i < 0 || i >= Hashers.Length) return null; return Hashers[i](); }

        // Generate a dbhash using the current default input and salt size
        public static byte[] GetHash(string input)
        {
            //Generate a cryptographic random number.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] salt = new byte[SALT_SIZE];
            rng.GetBytes(salt);

            byte[] hashOfInput = GetHashAlgo(CurrentAlgoIndex).ComputeHash(Encoding.UTF8.GetBytes(Convert.ToBase64String(salt) + input));

            byte[] dbhash = new byte[1 + SALT_SIZE + hashOfInput.Length];
            dbhash[0] = (byte)((SALT_SIZE << 4) | CurrentAlgoIndex);
            salt.CopyTo(dbhash, 1);
            hashOfInput.CopyTo(dbhash, 1 + SALT_SIZE);

            return dbhash;
        }

        // Hash the input using salt and algo of dbhash and compare the result to dbhash
        public static bool CheckPassword(byte[] dbhash, string input)
        {
            try
            {
                int salt_size = dbhash[0] >> 4;
                if (salt_size == 0) return false;
                var hash_algo = GetHashAlgo(dbhash[0] & 0xF);

                ArraySegment<byte> hash = new ArraySegment<byte>(dbhash, 1 + salt_size, dbhash.Length - 1 - salt_size);

                byte[] hashOfInput = GetHashAlgo(CurrentAlgoIndex).ComputeHash(Encoding.UTF8.GetBytes(Convert.ToBase64String(dbhash, 1, salt_size) + input));

                return hashOfInput.SequenceEqual(hash);
            }
            catch
            {
                return false;
            }
        }

        public static string GetHashString(byte[] dbhash)
        {
            int salt_size = (dbhash[0] >> 4) ^ 0xC;
            if (salt_size == 0) return null;
            var hash_algo = GetHashAlgo((dbhash[0] & 0xF) ^ 0xC);

            byte[] ret = new byte[dbhash.Length - 1 - salt_size];
            Array.Copy(dbhash, 1 + salt_size, ret, 0, ret.Length);
            return Convert.ToBase64String(ret);
        }

        public static bool CheckHashString(byte[] dbhash, string hashstr)
        {
            return GetHashString(dbhash) == hashstr;
        }
    }
}
