﻿using Android.App;
using Firebase;
using Firebase.Auth;

namespace Roadside.Services
{
    public class AuthenticationService : PhoneAuthProvider.OnVerificationStateChangedCallbacks, IAuthenticationService
    {
        private TaskCompletionSource<bool> _verificationCodeCompleteSource;
        private string _verificationID;

        public Task<bool> AuthenticateMobile(string mobile)
        {
            _verificationCodeCompleteSource = new TaskCompletionSource<bool>();

            // Ensure you have the current activity context
            Activity currentActivity = Platform.CurrentActivity;

            var authOption = PhoneAuthOptions.NewBuilder()
                .SetPhoneNumber(mobile)
                .SetTimeout((Java.Lang.Long)60L, Java.Util.Concurrent.TimeUnit.Seconds)
                .SetActivity(currentActivity)
                .SetCallbacks(this)
                .Build();

            PhoneAuthProvider.VerifyPhoneNumber(authOption);

            return _verificationCodeCompleteSource.Task;
        }

        public override void OnCodeSent(string verificationID, PhoneAuthProvider.ForceResendingToken p1)
        {
            base.OnCodeSent(verificationID, p1);
            _verificationCodeCompleteSource.SetResult(true);
            _verificationID = verificationID;
        }

        private void SaveAuthToken(string token)
        {
            SecureStorage.SetAsync("auth_token", token);
        }
        // Method to retrieve authentication token
        private async Task<string> GetAuthTokenAsync()
        {
            try
            {
                return await SecureStorage.GetAsync("auth_token");
            }
            catch (Exception ex)
            {
                // Handle possible exceptions here
               
            }
        }
        public override void OnVerificationCompleted(PhoneAuthCredential p0)
        {
            System.Diagnostics.Debug.WriteLine("Verification completed");

            // Automatically sign in the user (optional)
            FirebaseAuth.Instance.SignInWithCredentialAsync(p0)
                .ContinueWith((task) =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        _verificationCodeCompleteSource.SetResult(false);
                    }
                    else if (task.IsCompleted)
                    {
                        // Save the authentication token
                        SaveAuthToken(task.Result.User.Uid);
                        _verificationCodeCompleteSource.SetResult(true);
                    }
                });
        }

        public override void OnVerificationFailed(FirebaseException p0)
        {
            _verificationCodeCompleteSource.SetResult(false);
            System.Diagnostics.Debug.WriteLine("Verification failed: " + p0.Message);
        }

        public async Task<bool> ValidateOTP(string code)
        {
            bool returnValue = false;

            if (!string.IsNullOrEmpty(_verificationID))
            {
                var credential = PhoneAuthProvider.GetCredential(_verificationID, code);

                await FirebaseAuth.Instance.SignInWithCredentialAsync(credential)
                    .ContinueWith((authTask) =>
                    {
                        if (authTask.IsFaulted || authTask.IsCanceled)
                        {
                            returnValue = false;
                        }
                        else if (authTask.IsCompleted)
                        {
                            returnValue = true;
                        }
                    });
            }

            return returnValue;
        }
    }
}