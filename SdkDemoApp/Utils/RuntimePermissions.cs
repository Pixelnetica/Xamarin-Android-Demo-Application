using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Java.Util;
using System;
using System.Collections.Generic;

namespace App.Utils
{
    public class RuntimePermissions
    {
        public interface Callback
        {
            void OnRuntimePermission(Activity activity, string permission, bool granted);
        }

        class Task
        {
            public readonly string permission;
            public readonly Callback callback;
            public Task(string permission, Callback callback)
            {
                this.permission = permission;
                this.callback = callback;
            }
        }

        // Request array 
        private const int initialRequstValue = 100;
        private int requestCounter = initialRequstValue;
        private Dictionary<int, Task> requestList = new Dictionary<int, Task>();

        public int RunWithPermission(Activity activity, string permission, string message, Callback callback)
        {
            if (ContextCompat.CheckSelfPermission(activity, permission) != (int)Permission.Granted)
            {
                // Prepare task to handle IOnRequestPermissionsResultCallback
                int requestCode = requestCounter++;
                requestList.Add(requestCode, new Task(permission, callback));

                // Common action to use in Alert and directly
                Action requestPermission = delegate
                {
                    ActivityCompat.RequestPermissions(activity, new string[] { permission }, requestCode);
                };


                if (!string.IsNullOrEmpty(message) && ActivityCompat.ShouldShowRequestPermissionRationale(activity, permission))
                {
                    ApplicationInfo appInfo = activity.ApplicationInfo; // To get title and icon
                    var builer = new AlertDialog.Builder(activity);
                    builer.SetTitle(appInfo.LabelRes);
                    builer.SetIcon(appInfo.Icon);
                    builer.SetMessage(message);
                    builer.SetCancelable(true);

                    // Setup OK button
                    builer.SetPositiveButton(Android.Resource.String.Ok, (EventHandler<DialogClickEventArgs>)null);

                    // Display
                    var dialog = builer.Create();
                    dialog.Show();

                    // Add button handler
                    var button = dialog.GetButton((int)DialogButtonType.Positive);
                    button.Click += (sender, args) =>
                    {
                        dialog.Dismiss();
                        requestPermission();
                    };
                }
                else
                {
                    // Simple call
                    requestPermission();
                }
                return requestCode;
            }
            else
            {
                // Already done
                callback.OnRuntimePermission(activity, permission, true);
                return -1;
            }
        }

        public int RunWithPermission(Activity activity, string permission, int messageId, Callback callback)
        {
            string message = activity.GetString(messageId);
            return RunWithPermission(activity, permission, message, callback);
        }

        public bool HandleRequestPermissionsResult(Activity activity, int requestCode, string [] permissions, Permission[] grantResult)
        {
            // Get task for required request
            if (!requestList.ContainsKey(requestCode))
            {
                return false;
            }

            Task task = requestList[requestCode];

            // Cleanup task list
            requestList.Remove(requestCode);
            if (requestList.Count == 0)
            {
                requestCounter = initialRequstValue;
            }

            // Execute task
            Permission result = FindPermissionResult(task.permission, permissions, grantResult);
            if (result == PermissionNotFound)
            {
                return false;
            }

            task.callback.OnRuntimePermission(activity, task.permission, result == Permission.Granted);
            return true;
        }

        private const Permission PermissionNotFound = (Permission) int.MinValue;
        private static Permission FindPermissionResult(string permission, string [] request, Permission[] grantResult)
        {
            if (request.Length != grantResult.Length)
            {
                throw new InvalidOperationException("Permissions and results have different length");
            }

            int index = Array.FindIndex(request, s => object.Equals(s, permission));
            if (index != -1)
            {
                return grantResult[index];
            }
            else
            {
                return PermissionNotFound;
            }
        }
    }
}