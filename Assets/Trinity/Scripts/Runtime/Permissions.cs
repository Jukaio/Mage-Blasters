using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

namespace Alteruna.Trinity
{
    /// <summary>
    /// Class <c>Permissions</c> manages application access to platform specific permissions.
    /// </summary>
    public class Permissions : MonoBehaviour
    {
        [SerializeField]
        private bool RequestMicPermissions = false;

        [HideInInspector]
        public bool MicAuthorized = true;

        private void Start()
        {
            if (RequestMicPermissions)
            {
                #if PLATFORM_ANDROID
                    if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                    {
                        var callbacks = new PermissionCallbacks();
                        callbacks.PermissionGranted += AndroidPermissionGranted;
                        callbacks.PermissionDenied += AndroidPermissionDenied;
                        callbacks.PermissionDeniedAndDontAskAgain += AndroidPermissionDeniedAndDontAskAgain;
                        Permission.RequestUserPermission(Permission.Microphone, callbacks);
                    }
                #endif
            }
        }

        private void AndroidPermissionGranted(string permissionName)
        {
            #if PLATFORM_ANDROID
                switch (permissionName)
                {
                    case Permission.Microphone:
                        {
                            MicAuthorized = true;
                            break;
                        }

                    default:
                        break;
                }
            #endif
        }

        private void AndroidPermissionDenied(string permissionName)
        {
            #if PLATFORM_ANDROID
                switch (permissionName)
                {
                    case Permission.Microphone:
                        {
                            MicAuthorized = false;
                            break;
                        }

                    default:
                        break;
                }
            #endif
        }

        private void AndroidPermissionDeniedAndDontAskAgain(string permissionName)
        {
            #if PLATFORM_ANDROID
                switch (permissionName)
                {
                    case Permission.Microphone:
                        {
                            MicAuthorized = false;
                            // TODO: Save never ask again in playerPrefs
                            break;
                        }

                    default:
                        break;
                }
            #endif
        }
    }
}