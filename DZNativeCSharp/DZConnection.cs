using System;
using System.Runtime.InteropServices;
using System.Collections;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct dz_connect_configuration
{
    public dz_connect_configuration(string app_id,
        string product_id, string product_build_id,
        string user_profile_path, dz_connect_onevent_cb connect_event_cb,
        IntPtr anonymous_blob, dz_connect_crash_reporting_delegate app_has_crashed_delegate)
    {
        this.app_id = app_id;
        this.product_id = product_id;
        this.product_build_id = product_build_id;
        this.user_profile_path = user_profile_path;
        this.connect_event_cb = connect_event_cb;
        this.anonymous_blob = anonymous_blob;
        this.app_has_crashed_delegate = app_has_crashed_delegate;
    }

    [MarshalAs(UnmanagedType.LPStr)]
    public string app_id;

    [MarshalAs(UnmanagedType.LPStr)]
    public string product_id;

    [MarshalAs(UnmanagedType.LPStr)]
    public string product_build_id;

    [MarshalAs(UnmanagedType.LPStr)]
    public string user_profile_path;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public dz_connect_onevent_cb connect_event_cb;

    /* [MarshalAs(UnmanagedType.LPStr)] */
    public IntPtr anonymous_blob;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    public dz_connect_crash_reporting_delegate app_has_crashed_delegate;
};

public enum DZConnectionEvent
{
    UNKNOWN,
    USER_OFFLINE_AVAILABLE,
    USER_ACCESS_TOKEN_OK,
    USER_ACCESS_TOKEN_FAILED,
    USER_LOGIN_OK,
    USER_LOGIN_FAIL_NETWORK_ERROR,
    USER_LOGIN_FAIL_BAD_CREDENTIALS,
    USER_LOGIN_FAIL_USER_INFO,
    USER_LOGIN_FAIL_OFFLINE_MODE,
    USER_NEW_OPTIONS,
    ADVERTISEMENT_START,
    ADVERTISEMENT_STOP
};

public enum DZConnectStreamingMode
{
    UNKNOWN,
    ONEDEMAND,
    RADIO
};

public delegate void dz_activity_operation_callback(IntPtr d, IntPtr data, int status, int result);
public delegate void dz_connect_onevent_cb(IntPtr connectHandle, IntPtr eventHandle, IntPtr data);
public delegate bool dz_connect_crash_reporting_delegate();

[Serializable()]
public class ConnectionInitFailedException : System.Exception
{
    public ConnectionInitFailedException() : base() { }
    public ConnectionInitFailedException(string message) : base(message) { }
    public ConnectionInitFailedException(string message, System.Exception inner) : base(message, inner) { }
    protected ConnectionInitFailedException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
    { }
}

[Serializable()]
public class ConnectionRequestFailedException : System.Exception
{
    public ConnectionRequestFailedException() : base() { }
    public ConnectionRequestFailedException(string message) : base(message) { }
    public ConnectionRequestFailedException(string message, System.Exception inner) : base(message, inner) { }
    protected ConnectionRequestFailedException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
    { }
}

public class DZConnection
{
    public DZConnection(dz_connect_configuration config)
    {
        Handle = dz_connect_new(ref config);
        Active = false;
        if (Handle.ToInt32() == 0)
            throw new ConnectionInitFailedException("Connection handle failed to initialize. Check connection info you gave");
    }

    public void Activate(IntPtr context)
    {
        if (dz_connect_activate(Handle, context) != 0)
            throw new ConnectionRequestFailedException("Connection failed to activate.");
        Active = true;
    }

    public long GetDeviceId()
    {
        return dz_connect_get_device_id(Handle).ToInt64();
    }

    public void CachePathSet(string path, dz_activity_operation_callback cb = null, IntPtr operationUserdata = default(IntPtr))
    {
        if (dz_connect_cache_path_set(Handle, cb, operationUserdata, path) != 0)
            throw new ConnectionRequestFailedException("Cache path was not set. Check connection.");
    }

    public void SetAccessToken(string token, dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        if (dz_connect_set_access_token(Handle, cb, operationUserData, token) != 0)
            throw new ConnectionRequestFailedException("Could not set access token. Check connection and that the token is valid.");
    }

    public void SetOfflineMode(bool offlineModeForced, dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        if (dz_connect_offline_mode(Handle, cb, operationUserData, offlineModeForced) != 0)
            throw new ConnectionRequestFailedException("Failed to set offline mode.");
    }

    public void Shutdown(dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        if (Handle.ToInt64() != 0)
        {
            dz_connect_deactivate(Handle, cb, operationUserData);
            Active = false;
        }
    }

    public static DZConnectionEvent GetEventFromHandle(IntPtr handle)
    {
        return (DZConnectionEvent)dz_connect_event_get_type(handle);
    }

    public bool Active { get; set; }
    public IntPtr Handle { get; set; }

    [DllImport(DZImport.LibPath)] public static extern void dz_object_release(IntPtr objectHandle);
    [DllImport(DZImport.LibPath)] public static extern IntPtr dz_connect_new(ref dz_connect_configuration config);
    [DllImport(DZImport.LibPath)] public static extern IntPtr dz_connect_get_device_id(IntPtr self);
    [DllImport(DZImport.LibPath)] public static extern int dz_connect_activate(IntPtr self, IntPtr userdata);
    [DllImport(DZImport.LibPath)] public static extern int dz_connect_cache_path_set(IntPtr self, dz_activity_operation_callback cb, IntPtr data, [MarshalAs(UnmanagedType.LPStr)]string local_path);
    [DllImport(DZImport.LibPath)] public static extern int dz_connect_set_access_token(IntPtr self, dz_activity_operation_callback cb, IntPtr data, [MarshalAs(UnmanagedType.LPStr)]string token);
    [DllImport(DZImport.LibPath)] public static extern int dz_connect_offline_mode(IntPtr self, dz_activity_operation_callback cb, IntPtr data, bool offline_mode_forced);
    [DllImport(DZImport.LibPath)] public static extern int dz_connect_event_get_type(IntPtr eventHandle);
    [DllImport(DZImport.LibPath)] public static extern int dz_connect_deactivate(IntPtr connectHandle, dz_activity_operation_callback cb, IntPtr data);
}
