﻿//----------------------------------------------------------------------------------------------
// <copyright file="WindowsAuthenticationHelper.cs" company="SonarSource SA and Microsoft Corporation">
// Copyright (c) SonarSource SA and Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//----------------------------------------------------------------------------------------------

using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SonarQubeBootstrapper.Helpers
{
    /// <summary>
    /// Leverages the windows UI to prompt for a password
    /// </summary>
    internal static class WindowsAuthenticationHelper
    {
        public struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        [DllImport("credui")]
        private static extern CredUIReturnCodes CredUIPromptForCredentials(ref CREDUI_INFO creditUR,
              string targetName,
              IntPtr reserved1,
              int iError,
              StringBuilder userName,
              int maxUserName,
              StringBuilder password,
              int maxPassword,
              [MarshalAs(UnmanagedType.Bool)] ref bool pfSave,
              CREDUI_FLAGS flags);

        [Flags]
        enum CREDUI_FLAGS
        {
            INCORRECT_PASSWORD = 0x1,
            DO_NOT_PERSIST = 0x2,
            REQUEST_ADMINISTRATOR = 0x4,
            EXCLUDE_CERTIFICATES = 0x8,
            REQUIRE_CERTIFICATE = 0x10,
            SHOW_SAVE_CHECK_BOX = 0x40,
            ALWAYS_SHOW_UI = 0x80,
            REQUIRE_SMARTCARD = 0x100,
            PASSWORD_ONLY_OK = 0x200,
            VALIDATE_USERNAME = 0x400,
            COMPLETE_USERNAME = 0x800,
            PERSIST = 0x1000,
            SERVER_CREDENTIAL = 0x4000,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            USERNAME_TARGET_CREDENTIALS = 0x80000,
            KEEP_USERNAME = 0x100000,
        }

        public enum CredUIReturnCodes
        {
            NO_ERROR = 0,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_NOT_FOUND = 1168,
            ERROR_INVALID_ACCOUNT_NAME = 1315,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INVALID_FLAGS = 1004,
        }

        /// <summary>
        /// Prompts for password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns>True if no errors.</returns>
        internal static bool PromptForPassword(Session session, string user, out string password)
        {
            // Setup the flags and variables
            StringBuilder userPassword = new StringBuilder(), userID = new StringBuilder(user);
            CREDUI_INFO credUI = new CREDUI_INFO();
            credUI.cbSize = Marshal.SizeOf(credUI);
            bool save = false;
            CREDUI_FLAGS flags = CREDUI_FLAGS.ALWAYS_SHOW_UI | CREDUI_FLAGS.GENERIC_CREDENTIALS;

            CredUIReturnCodes returnCode = CredUIReturnCodes.NO_ERROR;

            bool validCredentials = false;

            do
            {
                // Prompt the user to enter the password
                returnCode = CredUIPromptForCredentials(ref credUI, "SonarQube", IntPtr.Zero, 0, userID, 100, userPassword, 100, ref save, flags);
                if (returnCode == CredUIReturnCodes.NO_ERROR)
                {
                    validCredentials = CredentialValidator.ValidateCredential(session, userID.ToString(), userPassword.ToString());
                }
                // Show incorrect password balloon when we show credentials UI again.
                flags |= CREDUI_FLAGS.INCORRECT_PASSWORD;
            }
            while (returnCode == CredUIReturnCodes.NO_ERROR && !validCredentials);

            user = userID.ToString();
            password = userPassword.ToString();

            return (returnCode == CredUIReturnCodes.NO_ERROR && validCredentials);
        }
    }
}