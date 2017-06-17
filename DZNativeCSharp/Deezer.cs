using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DZNativeCSharp
{
    public class Deezer {

        private IntPtr SelfPtr;
        public DZConnection Connection { get; private set; }
        public DZPlayer Player { get; private set; }
        public bool isPaused { get; private set; }
        public bool isStopped { get; private set; }
        public DZPlayerRepeatMode RepeatMode { get; private set; }
        public bool isShuffleMode { get; private set; }
        public List<Listener> Listeners = new List<Listener> ();
        public int IndexInPlaylist { get; private set; }

        public void Setup() {
            //contentLink = "track/10287076";
            //contentLink = "album/607845";
            // contentLink = "playlist/1363560485"; // FIXME: choose your content here
            string userAccessToken = "fr49mph7tV4KY3ukISkFHQysRpdCEbzb958dB320pM15OpFsQs";
            string userApplicationid = "190262";
            string userApplicationName = "UnityPlayer";
            string userApplicationVersion = "00001";
            // TODO: system-wise cache path

			string userCachePath = "c:\\dzr\\dzrcache_NDK_SAMPLE";

            dz_connect_configuration config = new dz_connect_configuration (
                userApplicationid,
                userApplicationName,
                userApplicationVersion,
                userCachePath,
                ConnectionOnEventCallback,
                IntPtr.Zero,
                null
            );
            IndexInPlaylist = 0;
            Connection = new DZConnection (config);
            GCHandle selfHandle = GCHandle.Alloc (this);
            SelfPtr = GCHandle.ToIntPtr(selfHandle);
            Player = new DZPlayer (Connection.Handle);
            Connection.Activate (SelfPtr);
            Player.Activate(SelfPtr);
            Player.SetEventCallback (PlayerOnEventCallback);
            Connection.CachePathSet(config.user_profile_path);
            Connection.SetAccessToken (userAccessToken);
            Connection.SetOfflineMode (false);

        }

        void DispatchEvent (DZPlayerEvent value, System.Object eventData) {
            foreach (Listener l in Listeners)
            	l.Notify (value, eventData);
        }

        public void Shutdown() {
            if (Player.Handle.ToInt64() != 0)
                Player.Shutdown (PlayerOnDeactivateCallback, SelfPtr);
            else if (Connection.Handle.ToInt64() != 0)
                Connection.Shutdown (ConnectionOnDeactivateCallback, SelfPtr);
        }

        public void Stop() {
            Player.Stop ();
            isPaused = false;
            isStopped = true;
        }

        public void PlayPause() {
            if (isStopped) {
                Player.Play ();
                isPaused = false;
                isStopped = false;
            } else if (isPaused) {
                Player.Resume ();
                isPaused = false;
            } else {
                Player.Pause ();
                isPaused = true;
            }
        }

        public void PlayNextTrack() {
            isPaused = false;
            isStopped = false;
            Int64 index = Marshal.SizeOf (IntPtr.Zero) == 4 ? DZPlayerIndex32.NEXT : DZPlayerIndex64.NEXT;
            Player.Play (command: DZPlayerCommand.NEXT, index: index);
        }

        public void PlayTrackAtIndex(int index) {
            isPaused = false;
            isStopped = false;
            Player.Play (command: DZPlayerCommand.JUMP_IN_TRACKLIST, index: index);
        }

        public void PlayPreviousTrack() {
            isPaused = false;
            isStopped = false;
            Int64 index = Marshal.SizeOf (IntPtr.Zero) == 4 ? DZPlayerIndex32.PREVIOUS : DZPlayerIndex64.PREVIOUS;
            Player.Play (command: DZPlayerCommand.PREV, index: index);
        }

        public void ToggleRepeatMode() {
            if (RepeatMode == DZPlayerRepeatMode.OFF)
                RepeatMode = DZPlayerRepeatMode.ALL;
            else if (RepeatMode == DZPlayerRepeatMode.ONE)
                RepeatMode = DZPlayerRepeatMode.OFF;
            else
                RepeatMode = DZPlayerRepeatMode.ONE;
            Player.UpdateRepeatMode (RepeatMode);
        }

        public void ToggleRandomMode() {
            isShuffleMode = !isShuffleMode;
            Player.EnableShuffleMode(isShuffleMode);
        }

        public void LoadContent(string content) {
            if (content == null || content.Length == 0)
                return;
            content = "dzmedia:///" + content;
            Player.Load (content);
        }

        public void PlaySongAtTimestamp(int seconds) {
            Player.Seek (seconds * 1000000);
        }

        /// <summary>
        /// Is called after a player event is thrown by the SDK. Is used to wait for and manage
        /// asynchronous events.
        /// </summary>
        /// <param name="handle">The player handle. See DZPlayer.</param>
        /// <param name="eventHandle">A pointer to a structure representing the event.</param>
        /// <param name="userData">A pointer to the context given when initializing the DZPlayer.</param>
        public static void PlayerOnEventCallback(IntPtr handle, IntPtr eventHandle, IntPtr userData) {
            // We get the object that was given as context from the IntPtr (in that case the Deezer itself)
            GCHandle selfHandle = GCHandle.FromIntPtr(userData);
            Deezer app = (Deezer)selfHandle.Target;

            DZPlayerEvent playerEvent = DZPlayer.GetEventFromHandle (eventHandle);
            app.IndexInPlaylist = app.Player.GetIndexInQueulist (eventHandle);
            switch (playerEvent) {
                case DZPlayerEvent.QUEUELIST_LOADED:
                    app.Player.Play (Cb);
                    break;
                case DZPlayerEvent.QUEUELIST_TRACK_RIGHTS_AFTER_AUDIOADS:
                    app.Player.PlayAudioAds ();
                    break;
                case DZPlayerEvent.RENDER_TRACK_END:
                    app.isStopped = true;
                    if (app.IndexInPlaylist == -1)
                        app.PlayPause ();
                    break;
            }

            app.DispatchEvent (playerEvent, app.IndexInPlaylist);	
        }

        private static void Cb(IntPtr intPtr, IntPtr data, int status, int result)
        {

        }

        /// <summary>
        /// Is called after a connection event is thrown by the SDK. Is used to wait for and manage
        /// asynchronous events.
        /// </summary>
        /// <param name="handle">The connection handle. See DZConnection.</param>
        /// <param name="eventHandle">A pointer to a structure representing the event.</param>
        /// <param name="userData">A pointer to the context given when initializing the DZConnection.</param>
        public static void ConnectionOnEventCallback(IntPtr handle, IntPtr eventHandle, IntPtr userData) {
            // We get the object that was given as context from the IntPtr (in that case the Deezer itself)
            GCHandle selfHandle = GCHandle.FromIntPtr(userData);
            Deezer app = (Deezer)(selfHandle.Target);

            DZConnectionEvent connectionEvent = DZConnection.GetEventFromHandle (eventHandle);
            //if (connectionEvent == DZConnectionEvent.USER_LOGIN_OK)
            if (connectionEvent == DZConnectionEvent.USER_LOGIN_FAIL_USER_INFO) {
                if (app.Player.Handle.ToInt64 () != 0)
                    app.Player.Shutdown (PlayerOnDeactivateCallback, app.SelfPtr);
                else if (app.Connection.Handle.ToInt64 () != 0)
                    app.Connection.Shutdown (ConnectionOnDeactivateCallback, app.SelfPtr);
            }

            app.LoadContent("track/10287076");
        }

        public static void PlayerOnDeactivateCallback(IntPtr delegateFunc, IntPtr operationUserData, int status, int result) {
            GCHandle selfHandle = GCHandle.FromIntPtr(operationUserData);
            Deezer app = (Deezer)(selfHandle.Target);
            app.Player.Active = false;
            app.Player.Handle = IntPtr.Zero;
            if (app.Connection.Handle.ToInt64() != 0)
                app.Connection.Shutdown (Deezer.ConnectionOnDeactivateCallback, operationUserData);
        }

        public static void ConnectionOnDeactivateCallback(IntPtr delegateFunc, IntPtr operationUserData, int status, int result) {
            GCHandle selfHandle = GCHandle.FromIntPtr(operationUserData);
            Deezer app = (Deezer)(selfHandle.Target);
            if (app.Connection.Handle.ToInt64() != 0) {
                app.Connection.Active = false;
                app.Connection.Handle = IntPtr.Zero;
            }
        }
    }
}
