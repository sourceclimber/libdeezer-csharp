using System;
using System.Runtime.InteropServices;
using System.Collections;

public enum DZPlayerEvent
{
    UNKNOWN,
    LIMITATION_FORCED_PAUSE,
    QUEUELIST_LOADED,
    QUEUELIST_NO_RIGHT,
    QUEUELIST_TRACK_NOT_AVAILABLE_OFFLINE,
    QUEUELIST_TRACK_RIGHTS_AFTER_AUDIOADS,
    QUEUELIST_SKIP_NO_RIGHT,
    QUEUELIST_TRACK_SELECTED,
    QUEUELIST_NEED_NATURAL_NEXT,
    MEDIASTREAM_DATA_READY,
    MEDIASTREAM_DATA_READY_AFTER_SEEK,
    RENDER_TRACK_START_FAILURE,
    RENDER_TRACK_START,
    RENDER_TRACK_END,
    RENDER_TRACK_PAUSED,
    RENDER_TRACK_SEEKING,
    RENDER_TRACK_UNDERFLOW,
    RENDER_TRACK_RESUMED,
    RENDER_TRACK_REMOVED
};

public enum DZPlayerCommand
{
    UNKNOWN,
    START_TRACKLIST,
    JUMP_IN_TRACKLIST,
    NEXT,
    PREV,
    DISLIKE,
    NATURAL_END,
    RESUMED_AFTER_ADS,
};

public enum DZPlayerRepeatMode
{
    OFF,
    ONE,
    ALL
};

public static class DZPlayerIndex32
{
    public static readonly Int32 INVALID = Int32.MaxValue;
    public static readonly Int32 PREVIOUS = Int32.MaxValue - 1;
    public static readonly Int32 CURRENT = Int32.MaxValue - 2;
    public static readonly Int32 NEXT = Int32.MaxValue - 3;
};

public static class DZPlayerIndex64
{
    public static readonly Int64 INVALID = Int64.MaxValue;
    public static readonly Int64 PREVIOUS = Int64.MaxValue - 1;
    public static readonly Int64 CURRENT = Int64.MaxValue - 2;
    public static readonly Int64 NEXT = Int64.MaxValue - 3;
};

public delegate void dz_player_onevent_cb(IntPtr playerHandle, IntPtr eventHandle, IntPtr data);
public delegate void dz_player_onindexprogress_cb(IntPtr playerHandle, uint progress, IntPtr data);

[Serializable()]
public class PlayerInitFailedException : System.Exception
{
    public PlayerInitFailedException() : base() { }
    public PlayerInitFailedException(string message) : base(message) { }
    public PlayerInitFailedException(string message, System.Exception inner) : base(message, inner) { }
    protected PlayerInitFailedException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
    { }
}

[Serializable()]
public class PlayerRequestFailedException : System.Exception
{
    public PlayerRequestFailedException() : base() { }
    public PlayerRequestFailedException(string message) : base(message) { }
    public PlayerRequestFailedException(string message, System.Exception inner) : base(message, inner) { }
    protected PlayerRequestFailedException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
    { }
}

public class DZPlayer
{
    public DZPlayer(IntPtr connectionHandle)
    {
        Active = false;
        Handle = dz_player_new(connectionHandle);
        if (Handle.ToInt64() == 0)
            throw new PlayerInitFailedException("Player failed to initialize. Check connection handle initialized properly");
    }

    public void Activate(IntPtr context)
    {
        if (dz_player_activate(Handle, context) != 0)
            throw new PlayerRequestFailedException("Unable to activate player. Check connection.");
        Active = true;
    }

    public void SetEventCallback(dz_player_onevent_cb cb)
    {
        if (dz_player_set_event_cb(Handle, cb) != 0)
            throw new PlayerRequestFailedException("Unable to set event callback for player.");
    }

    public void Load(string content = null, dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        currentContent = content;
        if (dz_player_load(Handle, cb, operationUserData, currentContent) != 0)
            throw new PlayerRequestFailedException("Unable to load content. Check the given dzmedia entry.");
    }

    public void Play(dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr),
        DZPlayerCommand command = DZPlayerCommand.START_TRACKLIST,
        Int64 index = -1)
    {
        if (index == -1)
            index = Marshal.SizeOf(IntPtr.Zero) == 4 ? DZPlayerIndex32.CURRENT : DZPlayerIndex64.CURRENT;
        if (dz_player_play(Handle, cb, operationUserData, (int)command, (uint)index) > 1)
            throw new PlayerRequestFailedException("Unable to play content.");
    }

    public void Seek(int microseconds, dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        if (Handle.ToInt64() != 0)
            dz_player_seek(Handle, cb, operationUserData, microseconds);
    }

    public void Shutdown(dz_activity_operation_callback cb = null, IntPtr operationuserData = default(IntPtr))
    {
        if (Handle.ToInt64() != 0)
            dz_player_deactivate(Handle, cb, operationuserData);
    }

    public void Stop(dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        if (dz_player_stop(Handle, cb, operationUserData) != 0)
            throw new PlayerRequestFailedException("Unable to stop current track.");
    }

    public void Pause(dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        if (dz_player_pause(Handle, cb, operationUserData) != 0)
            throw new PlayerRequestFailedException("Unable to pause current track.");
    }

    public void Resume(dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        if (dz_player_resume(Handle, cb, operationUserData) != 0)
            throw new PlayerRequestFailedException("Unable to resume current track.");
    }

    public void UpdateRepeatMode(DZPlayerRepeatMode mode, dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        dz_player_set_repeat_mode(Handle, cb, operationUserData, (int)mode);
    }

    public void EnableShuffleMode(bool shuffleMode, dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        dz_player_enable_shuffle_mode(Handle, cb, operationUserData, shuffleMode);
    }

    public void PlayAudioAds(dz_activity_operation_callback cb = null, IntPtr operationUserData = default(IntPtr))
    {
        if (dz_player_play_audioads(Handle, cb, operationUserData) != 0)
            throw new PlayerRequestFailedException("Unable to load audio ads.");
    }

    public int GetIndexInQueulist(IntPtr playerEventHandle)
    {
        int index = -1;
        int streaming_mode = (int)DZConnectStreamingMode.ONEDEMAND;
        dz_player_event_get_queuelist_context(playerEventHandle, ref streaming_mode, ref index);
        return index;
    }

    public static DZPlayerEvent GetEventFromHandle(IntPtr handle)
    {
        return (DZPlayerEvent)dz_player_event_get_type(handle);
    }

    public bool Active { get; set; }
    public IntPtr Handle { get; set; }
    private string currentContent = "";
    private int nbTracksPlayed;

    [DllImport(DZImport.LibPath)] private static extern IntPtr dz_player_new(IntPtr self);
    [DllImport(DZImport.LibPath)] private static extern int dz_player_activate(IntPtr player, IntPtr supervisor);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_deactivate(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_cache_next(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data, [MarshalAs(UnmanagedType.LPStr)]string trackUrl);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_load(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data, [MarshalAs(UnmanagedType.LPStr)]string tracklistData);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_pause(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_play(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data, int command, uint idx);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_play_audioads(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_stop(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_resume(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_seek(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data, uint pos);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_set_index_progress_cb(IntPtr player,
        dz_player_onindexprogress_cb cb, uint refreshTime);
    [DllImport(DZImport.LibPath)] private static extern int dz_player_set_event_cb(IntPtr player, dz_player_onevent_cb cb);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_set_repeat_mode(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data, int mode);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_enable_shuffle_mode(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data, bool shuffle_mode);
    [DllImport(DZImport.LibPath)] private static extern int dz_player_event_get_type(IntPtr eventHandle);
    [DllImport(DZImport.LibPath)]
    private static extern int dz_player_seek(IntPtr playerHandle,
        dz_activity_operation_callback cb, IntPtr data, int microseconds);
    [DllImport(DZImport.LibPath)] private static extern IntPtr dz_player_event_track_selected_dzapiinfo(IntPtr eventHandle);
    [DllImport(DZImport.LibPath)]
    private static extern IntPtr dz_player_event_track_selected_next_track_dzapiinfo(
        IntPtr eventHandle);
    [DllImport(DZImport.LibPath)] private static extern bool dz_player_event_track_selected_is_preview(IntPtr eventHandle);
    [DllImport(DZImport.LibPath)]
    private static extern bool dz_player_event_get_queuelist_context(IntPtr playerEventHandle,
        ref int out_streaming_mode, ref int index);
}
