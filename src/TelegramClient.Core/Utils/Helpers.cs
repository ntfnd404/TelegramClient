﻿using System;
using System.Security.Cryptography;
using TelegramClient.Core.MTProto.Crypto;

namespace TelegramClient.Core.Utils
{
    using BarsGroup.CodeGuard;

    using OpenTl.Schema;

    internal class TlHelpers
    {
        private static readonly Random Random = new Random();

        /// <summary>
        /// Generate <see cref="TVector{T}"/> with random long numbers
        /// </summary>
        /// <param name="length">Length of list</param>
        /// <returns>Returns a instance of <see cref="TVector{T}"/> with random long numbers</returns>
        /// TODO: Move to  TlHelpers?
        public static TVector<long> GenerateRandomTVectorLong(int length)
        {
            var randomIds = new TVector<long>();
            for (int i = 0; i < length; i++)
            {
                randomIds.Items.Add(TlHelpers.GenerateRandomLong());
            }

            return randomIds;
        }

        public static ulong GenerateRandomUlong()
        {
            var rand = ((ulong) Random.Next() << 32) | (ulong) Random.Next();
            return rand;
        }

        public static long GenerateRandomLong()
        {
            var rand = ((long) Random.Next() << 32) | Random.Next();
            return rand;
        }

        public static byte[] GenerateRandomBytes(int num)
        {
            var data = new byte[num];
            Random.NextBytes(data);
            return data;
        }

        public static AesKeyData CalcKey(byte[] sharedKey, byte[] msgKey, bool client)
        {
            Guard.That(sharedKey.Length, nameof(sharedKey)).IsEqual(256);
            Guard.That(msgKey.Length, nameof(msgKey)).IsEqual(16);

            var x = client ? 0 : 8;
            var buffer = new byte[48];

            Array.Copy(msgKey, 0, buffer, 0, 16); // buffer[0:16] = msgKey
            Array.Copy(sharedKey, x, buffer, 16, 32); // buffer[16:48] = authKey[x:x+32]
            var sha1A = Sha1(buffer); // sha1a = sha1(buffer)

            Array.Copy(sharedKey, 32 + x, buffer, 0, 16); // buffer[0:16] = authKey[x+32:x+48]
            Array.Copy(msgKey, 0, buffer, 16, 16); // buffer[16:32] = msgKey
            Array.Copy(sharedKey, 48 + x, buffer, 32, 16); // buffer[32:48] = authKey[x+48:x+64]
            var sha1B = Sha1(buffer); // sha1b = sha1(buffer)

            Array.Copy(sharedKey, 64 + x, buffer, 0, 32); // buffer[0:32] = authKey[x+64:x+96]
            Array.Copy(msgKey, 0, buffer, 32, 16); // buffer[32:48] = msgKey
            var sha1C = Sha1(buffer); // sha1c = sha1(buffer)

            Array.Copy(msgKey, 0, buffer, 0, 16); // buffer[0:16] = msgKey
            Array.Copy(sharedKey, 96 + x, buffer, 16, 32); // buffer[16:48] = authKey[x+96:x+128]
            var sha1D = Sha1(buffer); // sha1d = sha1(buffer)

            var key = new byte[32]; // key = sha1a[0:8] + sha1b[8:20] + sha1c[4:16]
            Array.Copy(sha1A, 0, key, 0, 8);
            Array.Copy(sha1B, 8, key, 8, 12);
            Array.Copy(sha1C, 4, key, 20, 12);

            var iv = new byte[32]; // iv = sha1a[8:20] + sha1b[0:8] + sha1c[16:20] + sha1d[0:8]
            Array.Copy(sha1A, 8, iv, 0, 12);
            Array.Copy(sha1B, 0, iv, 12, 8);
            Array.Copy(sha1C, 16, iv, 20, 4);
            Array.Copy(sha1D, 0, iv, 24, 8);

            return new AesKeyData(key, iv);
        }

        public static byte[] CalcMsgKey(byte[] data)
        {
            var msgKey = new byte[16];
            Array.Copy(Sha1(data), 4, msgKey, 0, 16);
            return msgKey;
        }

        public static byte[] CalcMsgKey(byte[] data, int offset, int limit)
        {
            var msgKey = new byte[16];
            Array.Copy(Sha1(data, offset, limit), 4, msgKey, 0, 16);
            return msgKey;
        }

        public static byte[] Sha1(byte[] data)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(data);
            }
        }

        public static byte[] Sha1(byte[] data, int offset, int limit)
        {
            using (var sha1 = SHA1.Create())
            {
                return sha1.ComputeHash(data, offset, limit);
            }
        }
    }
}