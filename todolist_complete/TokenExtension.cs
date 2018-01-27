//*********************************************************
//
//    Copyright (c) Microsoft. All rights reserved.
//    This code is licensed under the Microsoft Public License.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Text;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;
using Windows.Security.Credentials;

namespace todolist_complete
{
    public static class TokenExtension
    {
        /// <summary>
        /// Returns true when the authentication token for the current user is expired.
        /// </summary>
        /// <param name="client">The current MobileServiceClient instance</param>
        /// <returns>true when the token is expired; otherwise false.</returns>
        public static bool IsTokenExpired(this IMobileServiceClient client)
        {
            return CheckToken(client.CurrentUser.MobileServiceAuthenticationToken);
        }
        /// <summary>
        /// Returns true when the authentication token for the current user is expired.
        /// </summary>
        /// <param name="client">The current MobileServiceClient instance</param>
        /// <param name="token">Mobile Services authentication token to check.</param>
        /// <returns>true when the token is expired; otherwise false.</returns>
        public static bool IsTokenExpired(this IMobileServiceClient client, string token)
        {
            // Check for expired token.
            return CheckToken(token);
        }
        /// <summary>
        /// Returns true when the authentication token for the current user is expired.
        /// </summary>
        /// <param name="client">The current MobileServiceClient instance</param>
        /// <param name="credential">PasswordCredential to check.</param>
        /// <returns>true when the token is expired; otherwise false.</returns>
        public static bool IsTokenExpired(this IMobileServiceClient client, PasswordCredential credential)
        {
            // Get the stored password.
            credential.RetrievePassword();

            // Check for expired token.
            return CheckToken(credential.Password);
        }
        private static bool CheckToken(string token)
        { 
            // Check for a signed-in user.
            if (String.IsNullOrEmpty(token))
            {
                // Raise an exception if there is no token.
                throw new InvalidOperationException(
                    "The client isn't signed-in or the token value isn't set.");
            }

            // Get just the JWT part of the token.
            var jwt = token.Split(new Char[] { '.' })[1];

            // Undo the URL encoding.
            jwt = jwt.Replace('-', '+');
            jwt = jwt.Replace('_', '/');
            switch (jwt.Length % 4)
            {
                case 0: break;
                case 2: jwt += "=="; break;
                case 3: jwt += "="; break;
                default: throw new System.Exception(
                    "The base64url string is not valid.");
            }

            // Decode the bytes from base64 and write to a JSON string.
            var bytes = Convert.FromBase64String(jwt);
            string jsonString = UTF8Encoding.UTF8.GetString(bytes, 0, bytes.Length);

            // Parse as JSON object and get the exp field value, 
            // which is the expiration date as a JavaScript primative date.
            JObject jsonObj = JObject.Parse(jsonString);
            var exp = Convert.ToDouble(jsonObj["exp"].ToString());

            // Calculate the expiration by adding the exp value (in seconds) to the 
            // base date of 1/1/1970.
            DateTime minTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var expire = minTime.AddSeconds(exp);

            // If the expiration date is less than now, the token is expired and we return true.
            return expire < DateTime.UtcNow ? true : false;
        }
    }
}
