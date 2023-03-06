using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using System;
using TMPro;

public class AccountManager : MonoBehaviour
{
    /// <summary>
    /// This script is responsible for integrating firebase User authentication with the game.
    /// This script  is resposible for signup, login and resetting password, email verification as well.
    /// </summary>

    [Header("Firebase Variables")]
    [SerializeField] private DependencyStatus dependencyStatus;
    [SerializeField] private FirebaseAuth auth;
    [SerializeField] private FirebaseUser user;

    [Header("Input Fields")]
    [SerializeField] private InputField login_email;
    [SerializeField] private TMP_InputField login_password;
    [SerializeField] private InputField reset_pass_email;
    [SerializeField] private InputField signup_username;
    [SerializeField] private InputField signup_email;
    [SerializeField] private TMP_InputField signup_password;
    [SerializeField] private TMP_InputField signup_verify_password;

    //This Text Field is to give information to the user, like wrong password, or any other information
    [Header("Texts")]
    [SerializeField] private Text Debug_Error;

    void Start()
    {
        AutoLoginFun();
    }

    #region Public Functions

    public void FirebaseLogin()
    {
        StartCoroutine(LoginCoroutine(login_email.text, login_password.text));
    }

    public void FirebaseRegistration()
    {
        StartCoroutine(RegisterCoroutine(signup_username.text, signup_email.text, signup_password.text));
    }

    public void ResetPassword()
    {
        StartCoroutine(SendResetEmail());
    }

    public void AutoLoginFun()
    {
        StartCoroutine(CheckAndFixDependencies());
    }

    #endregion

    #region Coroutine Functions

    private IEnumerator CheckAndFixDependencies()
    {
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(predicate: () => dependencyTask.IsCompleted);
        var result = dependencyTask.Result;
        if (result == DependencyStatus.Available)
        {
            auth = FirebaseAuth.DefaultInstance;
            user = auth.CurrentUser;
            yield return new WaitForEndOfFrame();
            StartCoroutine(CheckForAutoLogin());
        }
        else
            Debug.LogError("Awake Error: Dependency Status" + dependencyStatus);
    }

    private IEnumerator CheckForAutoLogin()
    {
        if(user != null)
        {
            var ReloadUserTask = user.ReloadAsync();
            yield return new WaitUntil(predicate: () => ReloadUserTask.IsCompleted);
            AutoLogin();
        }
        else
        {
            //Open Login Panel
        }
    }

    private IEnumerator LoginCoroutine(string email, String password)
    {
        var task = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null)
        {
            FirebaseException exception = task.Exception.GetBaseException() as FirebaseException;
            AuthError errorcode = (AuthError)exception.ErrorCode;
            switch (errorcode)
            {
                case AuthError.MissingEmail:
                    Debug_Error.text = "     Missing Email";
                    ClearLoginSession();
                    break;
                case AuthError.MissingPassword:
                    Debug_Error.text = "     Missing Password";
                    ClearLoginSession();
                    break;
                case AuthError.InvalidEmail:
                    Debug_Error.text = "     Invalid Email";
                    ClearLoginSession();
                    break;
                case AuthError.WrongPassword:
                    Debug_Error.text = "     Wrong Password";
                    ClearLoginSession();
                    break;
                case AuthError.UserNotFound:
                    Debug_Error.text = " No Account on this email";
                    ClearLoginSession();
                    break;
            }

        }
        else
        {
            if (task.Result.IsEmailVerified)
            {
                user = task.Result;
                Debug_Error.text = "Successful Connection !";
                ClearLoginSession();
                //Player logged in, now do the rest like, play the game
            }
            else
            {
                Debug_Error.text = "Email Not Verified";
            }
        }
    }

    private IEnumerator RegisterCoroutine(string user_name, string user_email, string user_pass)
    {
        if (user_name == null)
        {
            Debug_Error.text = "Please mention user name";
        }
        else if (user_pass != signup_verify_password.text)
        {
            Debug_Error.text = "The passwords does not match";
        }
        else
        {
            var task = auth.CreateUserWithEmailAndPasswordAsync(user_email, user_pass);

            yield return new WaitUntil(predicate: () => task.IsCompleted);
            if (task.Exception != null)
            {
                Debug_Error.text = task.Exception.Message;
                FirebaseException exception = task.Exception.GetBaseException() as FirebaseException;
                AuthError error_code = (AuthError)exception.ErrorCode;
                switch (error_code)
                {
                    case AuthError.EmailAlreadyInUse:
                        Debug_Error.text = "Email Already in use";
                        ClearRegisterSession();
                        break;
                    case AuthError.InvalidEmail:
                        Debug_Error.text = "Invalid Email";
                        ClearRegisterSession();
                        break;
                    case AuthError.MissingPassword:
                        Debug_Error.text = "Missing Password";
                        break;
                }
            }
            else
            {
                StartCoroutine(SendEmailVerification(user_email, task.Result));

                UserProfile profile = new UserProfile { DisplayName = signup_username.text };
                var updateTask = task.Result.UpdateUserProfileAsync(profile);

                yield return new WaitUntil(predicate: () => updateTask.IsCompleted);

                if (updateTask.Exception != null)
                {
                    Debug_Error.text = updateTask.Exception.Message;
                    ClearRegisterSession();
                }
                else
                {
                    ClearRegisterSession();
                }
            }
        }
    }

    private IEnumerator SendEmailVerification(string EmailToSend, FirebaseUser user)
    {

        var VerificationTask = user.SendEmailVerificationAsync();
        yield return new WaitUntil(predicate: () => VerificationTask.IsCompleted);

        if (VerificationTask.Exception != null)
        {
            FirebaseException exception = (FirebaseException)VerificationTask.Exception.GetBaseException();
            AuthError error_code = (AuthError)exception.ErrorCode;
            switch (error_code)
            {
                case AuthError.Cancelled:
                    Debug_Error.text = "Process Cancelled";
                    break;
                case AuthError.Failure:
                    Debug_Error.text = "Process Failed";
                    break;
                case AuthError.InvalidEmail:
                    Debug_Error.text = "Invalid Email";
                    break;
            }
        }
     
    }

    private IEnumerator SendResetEmail()
    {
        var task = auth.SendPasswordResetEmailAsync(reset_pass_email.text);
        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null)
        {
            FirebaseException exception = task.Exception.GetBaseException() as FirebaseException;
            AuthError error_code = (AuthError)exception.ErrorCode;
            Debug_Error.text = error_code.ToString();
        }
        else
        {
            Debug_Error.text = "Email Sent Successfully";

        }
    }

    #endregion

    #region Private Functions

    private void ClearRegisterSession()
    {
        signup_email.text = "";
        signup_password.text = "";
        signup_username.text = "";
        signup_verify_password.text = "";
    }

    private void ClearLoginSession()
    {
        login_email.text = "";
        login_password.text = "";
    }

    private void AutoLogin()
    {
        if (user != null)
        {
            // Rest things to do if the user is logged in.
        }
        else
        {
            //Not logged In, so Open Login Panel.
        }
    }

    #endregion
}
