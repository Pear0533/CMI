/*---------------------------*/
/* ECN Media Player
 * Copyright Edward Nutting © 2010
 * Please do not delete this! 
 * 
 * This class is open to use and adaptation, however, if you could spare
 * the time to email me with the code change or the concept of what you
 * change I would greatly appreciate it.
 * 
 * Email : EdMan196@hotmail.co.uk
 */
/*---------------------------*/

using System;
using System.Windows.Forms;
using WMPLib;

namespace ECN.MediaPlayer
{
    internal enum Players
    {
        NULL,
        Player1,
        Player2
    }
    /// <summary>
    ///     The different states the player can be in.
    /// </summary>
    public enum PlayerStates
    {
        NULL,
        Stopped,
        Paused,
        Playing,
        Loading,
        Ready,
        Error
    }
    internal enum FadeStates
    {
        In,
        Out,
        Down,
        Up
    }

    public class MediaPlayer
    {
        public delegate void ErrorEvent(ErrorEventArgs e);
        public delegate void PlayerMediaEndedEvent(PlayerMediaEndedEventArgs e);
        public delegate void PlayerPausedEvent(PlayerPausedEventArgs e);

        public delegate void PlayerStartEvent(PlayerStartEventArgs e);
        public delegate void PlayerStopEvent(PlayerStopEventArgs e);
        private const int CrossfadeTimerTickTime = 50;
        private const int FadeTimerTickTime = 500;
        private static int BeforeCrossfadeVolume;

        private readonly Timer CrossfadeTimer = new Timer();
        private readonly Timer FadeTimer = new Timer();

        private readonly WindowsMediaPlayerClass Player1 = new WindowsMediaPlayerClass();
        private readonly WindowsMediaPlayerClass Player2 = new WindowsMediaPlayerClass();

        /// <summary>
        ///     Whether the player should crossfade or not.
        /// </summary>
        public bool Crossfade;
        /// <summary>
        ///     The length of time to crossfade over.
        /// </summary>
        public int CrossfadeTime = 5;

        private int CrossfadeTotalRunTime;

        private float CrossfadeVolumeAdjustment;

        private FadeStates FadeState = FadeStates.In;
        /// <summary>
        ///     The length of time to fade over.
        /// </summary>
        public int FadeTime = 5;
        private int FadeTotalRunTime;
        private float FadeVolumeAdjustment;

        private bool InCrossfade;
        private bool InFade;

        private int NewVolume;

        private string Player1Song;

        private PlayerStates Player1State = PlayerStates.NULL;
        private string Player2Song;
        private PlayerStates Player2State = PlayerStates.NULL;

        public PlayerStates PlayerState = PlayerStates.NULL;

        private Players PlayingPlayer = Players.NULL;
        private Players StoppingPlayer = Players.NULL;

        private int volume;

        public MediaPlayer()
        {
            Init();
        }

        public MediaPlayer(int InitVolume, bool UseCrossfade)
        {
            Init();
            Crossfade = UseCrossfade;
            Volume = InitVolume;
        }

        public MediaPlayer(int InitVolume, bool UseCrossfade, int TheCrossfadeTime)
        {
            Init();
            Crossfade = UseCrossfade;
            CrossfadeTime = TheCrossfadeTime;
            Volume = InitVolume;
        }

        /// <summary>
        ///     The filename of the current playing track.
        /// </summary>
        public string CurrentSong
        {
            get
            {
                try
                {
                    switch (PlayingPlayer)
                    {
                        case Players.Player1:
                        {
                            return Player1Song;
                        }
                            break;
                        case Players.Player2:
                        {
                            return Player2Song;
                        }
                            break;
                    }
                    switch (StoppingPlayer)
                    {
                        case Players.Player1:
                        {
                            return Player1Song;
                        }
                            break;
                        case Players.Player2:
                        {
                            return Player2Song;
                        }
                            break;
                    }
                }
                catch { }
                return null;
            }
            set
            {
                switch (PlayingPlayer)
                {
                    case Players.Player1:
                        Player1Song = value;
                        break;
                    case Players.Player2:
                        Player2Song = value;
                        break;
                }
            }
        }

        /// <summary>
        ///     The position of the player in seconds. Can be set to change the position.
        ///     Can only be set when player is playing or paused.
        ///     Cannot be set if player is performing fade or crossfade.
        /// </summary>
        public double Position
        {
            get
            {
                try
                {
                    switch (PlayingPlayer)
                    {
                        case Players.Player1:
                        {
                            return Player1.currentPosition;
                        }
                            break;
                        case Players.Player2:
                        {
                            return Player2.currentPosition;
                        }
                            break;
                    }
                    switch (StoppingPlayer)
                    {
                        case Players.Player1:
                        {
                            return Player1.currentPosition;
                        }
                            break;
                        case Players.Player2:
                        {
                            return Player2.currentPosition;
                        }
                            break;
                    }
                }
                catch { }
                return -1;
            }
            set
            {
                if (PlayerState == PlayerStates.Playing || PlayerState == PlayerStates.Paused)
                {
                    switch (PlayingPlayer)
                    {
                        case Players.Player1:
                            Player1.currentPosition = value;
                            break;
                        case Players.Player2:
                            Player2.currentPosition = value;
                            break;
                    }
                }
            }
        }
        /// <summary>
        ///     The duration of the current song in seconds.
        /// </summary>
        public double Duration
        {
            get
            {
                try
                {
                    switch (PlayingPlayer)
                    {
                        case Players.Player1:
                        {
                            return Player1.currentMedia.duration;
                        }
                            break;
                        case Players.Player2:
                        {
                            return Player2.currentMedia.duration;
                        }
                            break;
                    }
                    switch (StoppingPlayer)
                    {
                        case Players.Player1:
                        {
                            return Player1.currentMedia.duration;
                        }
                            break;
                        case Players.Player2:
                        {
                            return Player2.currentMedia.duration;
                        }
                            break;
                    }
                }
                catch { }
                return 0;
            }
        }
        /// <summary>
        ///     The volume the player should play at.
        ///     Cannot be set if player is performing fade or crossfade.
        /// </summary>
        public int Volume
        {
            get => volume;
            set
            {
                if (value <= 100 && value >= 0)
                {
                    volume = value;
                }
                else if (volume >= 100)
                {
                    volume = 100;
                }
                else
                {
                    volume = 0;
                }
                if (!InCrossfade && !InFade)
                {
                    switch (PlayingPlayer)
                    {
                        case Players.Player1:
                            Player1.volume = volume;
                            break;
                        case Players.Player2:
                            Player2.volume = volume;
                            break;
                        case Players.NULL:
                            Player1.volume = volume;
                            Player2.volume = volume;
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     Whether the player is fading/crossfading.
        /// </summary>
        public bool inFade => InFade || InCrossfade;

        /// <summary>
        ///     Fired when player starts playing.
        /// </summary>
        public event PlayerStartEvent OnPlayerStart;
        /// <summary>
        ///     Fired when player stops.
        /// </summary>
        public event PlayerStopEvent OnPlayerStop;
        /// <summary>
        ///     Fires when player pauses.
        /// </summary>
        public event PlayerPausedEvent OnPlayerPaused;
        /// <summary>
        ///     Fired when player media ends.
        /// </summary>
        public event PlayerMediaEndedEvent OnPlayerMediaEnd;

        private event PlayerReadyEvent OnPlayerReady;
        private event PlayerErrorEvent OnPlayerError;

        /// <summary>
        ///     Fired when an error occurs.
        /// </summary>
        public event ErrorEvent OnError;

        private void Init()
        {
            Player1.PlayStateChange += Player1_PlayStateChange;
            Player2.PlayStateChange += Player2_PlayStateChange;
            OnPlayerStart += MediaPlayer_OnPlayerStart;
            OnPlayerStop += MediaPlayer_OnPlayerStop;
            OnPlayerPaused += MediaPlayer_OnPlayerPaused;
            OnPlayerReady += MediaPlayer_OnPlayerReady;
            OnPlayerMediaEnd += MediaPlayer_OnPlayerMediaEnd;
            OnPlayerError += MediaPlayer_OnPlayerError;
            CrossfadeTimer.Interval = CrossfadeTimerTickTime;
            CrossfadeTimer.Enabled = false;
            CrossfadeTimer.Tick += CrossfadeTimer_Tick;
            FadeTimer.Interval = FadeTimerTickTime;
            FadeTimer.Enabled = false;
            FadeTimer.Tick += FadeTimer_Tick;
        }

        private void MediaPlayer_OnPlayerStart(PlayerStartEventArgs e)
        {
            try
            {
                switch (e.ThePlayer)
                {
                    case Players.Player1:
                        Player1State = PlayerStates.Playing;
                        break;
                    case Players.Player2:
                        Player2State = PlayerStates.Playing;
                        break;
                }
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("OnPlayerStart : " + ex.Message));
            }
        }

        private void MediaPlayer_OnPlayerStop(PlayerStopEventArgs e)
        {
            try
            {
                switch (e.ThePlayer)
                {
                    case Players.Player1:
                        Player1State = PlayerStates.Stopped;
                        break;
                    case Players.Player2:
                        Player2State = PlayerStates.Stopped;
                        break;
                }
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("OnPlayerStop : " + ex.Message));
            }
        }

        private void MediaPlayer_OnPlayerPaused(PlayerPausedEventArgs e)
        {
            try
            {
                switch (e.ThePlayer)
                {
                    case Players.Player1:
                        Player1State = PlayerStates.Paused;
                        break;
                    case Players.Player2:
                        Player2State = PlayerStates.Paused;
                        break;
                }
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("OnPlayerPaused : " + ex.Message));
            }
        }

        private void MediaPlayer_OnPlayerReady(PlayerReadyEventArgs e)
        {
            try
            {
                switch (e.ThePlayer)
                {
                    case Players.Player1:
                        Player1State = PlayerStates.Ready;
                        break;
                    case Players.Player2:
                        Player2State = PlayerStates.Ready;
                        break;
                }
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("OnPlayerPaused : " + ex.Message));
            }
        }

        private void MediaPlayer_OnPlayerMediaEnd(PlayerMediaEndedEventArgs e)
        {
            try { }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("OnPlayerMediaEnded : " + ex.Message));
            }
        }

        private void MediaPlayer_OnPlayerError(PlayerErrorEventArgs e)
        {
            try
            {
                switch (e.ThePlayer)
                {
                    case Players.Player1:
                        Player1State = PlayerStates.Error;
                        break;
                    case Players.Player2:
                        Player2State = PlayerStates.Error;
                        break;
                }
                OnError.Invoke(new ErrorEventArgs("Player " + e.ThePlayer + " error - StateNum = " + e.StateNum));
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("OnPlayerError : " + ex.Message));
            }
        }

        private void Player1_PlayStateChange(int NewState)
        {
            PlayerStateChange(NewState, Player1Song, Players.Player1);
        }

        private void Player2_PlayStateChange(int NewState)
        {
            PlayerStateChange(NewState, Player2Song, Players.Player2);
        }

        private void PlayerStateChange(int NewState, string CurrentSong, Players ThePlayer)
        {
            try
            {
                switch (NewState)
                {
                    case 0: // Undefined
                        OnPlayerError.Invoke(new PlayerErrorEventArgs(ThePlayer, 0));
                        break;
                    case 1: // Stopped
                        if (!Crossfade && !InFade)
                        {
                            OnPlayerStop.Invoke(new PlayerStopEventArgs(CurrentSong, ThePlayer));
                        }
                        break;
                    case 2: // Paused
                        OnPlayerPaused.Invoke(new PlayerPausedEventArgs(CurrentSong, ThePlayer));
                        break;
                    case 3: // Playing
                        OnPlayerStart.Invoke(new PlayerStartEventArgs(CurrentSong, ThePlayer));
                        break;
                    case 4: // ScanForward
                        break;
                    case 5: // ScanReverse
                        break;
                    case 6: // Buffering
                        switch (ThePlayer)
                        {
                            case Players.Player1:
                                Player1State = PlayerStates.Loading;
                                break;
                            case Players.Player2:
                                Player2State = PlayerStates.Loading;
                                break;
                        }
                        break;
                    case 7: // Waiting
                        break;
                    case 8: // MediaEnded
                        OnPlayerMediaEnd.Invoke(new PlayerMediaEndedEventArgs(CurrentSong));
                        break;
                    case 9: // Transitioning
                        break;
                    case 10: // Ready
                        OnPlayerReady.Invoke(new PlayerReadyEventArgs(ThePlayer));
                        break;
                    case 11: // Reconnecting
                        break;
                    case 12: // Last
                        break;
                    default:
                        OnPlayerError.Invoke(new PlayerErrorEventArgs(ThePlayer, -1));
                        break;
                }
            }
            catch { }
        }

        /// <summary>
        ///     Starts the player playing the specified file.
        /// </summary>
        /// <param name="TheSong">The filename of the song to play.</param>
        /// <param name="Fade">Whether to fade or not.</param>
        /// <returns>Returns true if player starts playing</returns>
        public bool Play(string TheSong, bool Fade)
        {
            try
            {
                if (PlayingPlayer == Players.NULL)
                {
                    if (Fade)
                    {
                        PlayingPlayer = Players.Player1;
                        Player1Song = TheSong;
                        Player1.URL = TheSong;
                        Player1.volume = 0;
                        FadeState = FadeStates.In;
                        StartFade();
                    }
                    else
                    {
                        PlayingPlayer = Players.Player1;
                        Player1Song = TheSong;
                        Player1.URL = TheSong;
                        Player1.volume = Volume;
                        Player1.play();
                        PlayerState = PlayerStates.Playing;
                        return true;
                    }
                }
                else if (Crossfade)
                {
                    switch (PlayingPlayer)
                    {
                        case Players.Player1:
                            Player2Song = TheSong;
                            StoppingPlayer = Players.Player1;
                            StartCrossfade();
                            return true;
                        case Players.Player2:
                            Player1Song = TheSong;
                            StoppingPlayer = Players.Player2;
                            StartCrossfade();
                            return true;
                        case Players.NULL:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("Play : " + ex.Message));
            }
            return false;
        }

        /// <summary>
        ///     Starts the player playing the paused file.
        /// </summary>
        /// <param name="Fade">Whether to fade or not.</param>
        /// <returns>Returns true if player starts playing</returns>
        public bool Resume(bool Fade)
        {
            try
            {
                if (PlayerState == PlayerStates.Paused)
                {
                    if (!InFade && !InCrossfade)
                    {
                        if (Fade)
                        {
                            FadeState = FadeStates.In;
                            switch (StoppingPlayer)
                            {
                                case Players.Player1:
                                    PlayingPlayer = Players.Player1;
                                    break;
                                case Players.Player2:
                                    PlayingPlayer = Players.Player2;
                                    break;
                            }
                            StartFade();
                        }
                        else
                        {
                            switch (PlayingPlayer)
                            {
                                case Players.Player1:
                                    PlayingPlayer = Players.Player1;
                                    Player1.play();
                                    break;
                                case Players.Player2:
                                    PlayingPlayer = Players.Player2;
                                    Player2.play();
                                    break;
                            }
                        }
                        PlayerState = PlayerStates.Playing;
                    }
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        ///     Stops the player playing.
        /// </summary>
        /// <param name="Fade">Whether to fade or not.</param>
        /// <returns>Returns true if player stops playing</returns>
        public bool Stop(bool Fade)
        {
            try
            {
                StoppingPlayer = Players.Player2;
                PlayingPlayer = Players.NULL;
                Player1.stop();
                Player2.stop();
                PlayerState = PlayerStates.Stopped;
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("Stop : " + ex.Message));
            }
            return false;
        }

        /// <summary>
        ///     Pauses the player.
        /// </summary>
        /// <param name="Fade">Whether to fade or not.</param>
        /// <returns>Returns true if player pauses playing</returns>
        public bool Pause(bool Fade)
        {
            try
            {
                if (!InFade && !InCrossfade)
                {
                    if (PlayerState == PlayerStates.Playing)
                    {
                        if (Fade)
                        {
                            switch (PlayingPlayer)
                            {
                                case Players.Player1:
                                    StoppingPlayer = Players.Player1;
                                    PlayingPlayer = Players.NULL;
                                    break;
                                case Players.Player2:
                                    StoppingPlayer = Players.Player2;
                                    PlayingPlayer = Players.NULL;
                                    break;
                            }
                            FadeState = FadeStates.Out;
                            StartFade();
                        }
                        else
                        {
                            switch (PlayingPlayer)
                            {
                                case Players.Player1:
                                    Player1.pause();
                                    break;
                                case Players.Player2:
                                    Player2.pause();
                                    break;
                            }
                        }
                        PlayerState = PlayerStates.Paused;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("Pause : " + ex.Message));
            }
            return false;
        }

        private void StartCrossfade()
        {
            FadeTimer.Stop();
            BeforeCrossfadeVolume = PlayingPlayer == Players.Player1 ? Player1.volume : Player2.volume;
            InCrossfade = true;
            CrossfadeVolumeAdjustment = Volume / (float)CrossfadeTime;
            CrossfadeTotalRunTime = 0;
            PlayerState = PlayerStates.Playing;
            CrossfadeTimer.Enabled = true;
            CrossfadeTimer.Start();
        }

        private void CrossfadeTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (CrossfadeTotalRunTime <= 0)
                {
                    switch (StoppingPlayer)
                    {
                        case Players.Player1:
                            PlayingPlayer = Players.Player2;
                            Player2.volume = 0;
                            Player2.URL = Player2Song;
                            Player2.play();
                            break;
                        case Players.Player2:
                            PlayingPlayer = Players.Player1;
                            Player1.volume = 0;
                            Player1.URL = Player1Song;
                            Player1.play();
                            break;
                    }
                }
                else if (CrossfadeTotalRunTime <= CrossfadeTime * 1000)
                {
                    switch (PlayingPlayer)
                    {
                        case Players.Player2:
                            Player1.volume = (int)(BeforeCrossfadeVolume - CrossfadeVolumeAdjustment * ((float)CrossfadeTotalRunTime / 1000));
                            Player2.volume = (int)(CrossfadeVolumeAdjustment * ((float)CrossfadeTotalRunTime / 1000));
                            break;
                        case Players.Player1:
                            Player2.volume = (int)(BeforeCrossfadeVolume - CrossfadeVolumeAdjustment * ((float)CrossfadeTotalRunTime / 1000));
                            Player1.volume = (int)(CrossfadeVolumeAdjustment * ((float)CrossfadeTotalRunTime / 1000));
                            break;
                    }
                }
                else if (CrossfadeTotalRunTime >= CrossfadeTime * 1000)
                {
                    CrossfadeTimer.Enabled = false;
                    CrossfadeTimer.Stop();
                    InCrossfade = false;
                    switch (PlayingPlayer)
                    {
                        case Players.Player1:
                            Player1.volume = Volume;
                            Player2.volume = 0;
                            StoppingPlayer = Players.Player1;
                            break;
                        case Players.Player2:
                            Player2.volume = Volume;
                            Player1.volume = 0;
                            StoppingPlayer = Players.Player2;
                            break;
                    }
                }
                CrossfadeTotalRunTime += CrossfadeTimerTickTime;
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("Crossfade timer tick : " + ex.Message));
            }
        }

        private void StartFade()
        {
            InFade = true;
            FadeTotalRunTime = 0;
            switch (FadeState)
            {
                case FadeStates.In:
                    FadeVolumeAdjustment = Volume / (float)FadeTime;
                    PlayerState = PlayerStates.Playing;
                    break;
                case FadeStates.Out:
                    FadeVolumeAdjustment = Volume / (float)FadeTime;
                    PlayerState = PlayerStates.Stopped;
                    break;
                case FadeStates.Up:
                    FadeVolumeAdjustment = (NewVolume - Volume) / (float)FadeTime;
                    PlayerState = PlayerStates.Playing;
                    break;
                case FadeStates.Down:
                    FadeVolumeAdjustment = (Volume - NewVolume) / (float)FadeTime;
                    PlayerState = PlayerStates.Playing;
                    break;
            }
            FadeTimer.Enabled = true;
            FadeTimer.Start();
        }

        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (FadeState == FadeStates.In)
                {
                    if (FadeTotalRunTime <= 0)
                    {
                        switch (PlayingPlayer)
                        {
                            case Players.Player1:
                                Player1.volume = 0;
                                Player1.play();
                                break;
                            case Players.Player2:
                                Player2.volume = 0;
                                Player2.play();
                                break;
                        }
                    }
                    else if (FadeTotalRunTime < FadeTime * 1000)
                    {
                        switch (PlayingPlayer)
                        {
                            case Players.Player1:
                            {
                                Player1.volume = (int)(FadeVolumeAdjustment * ((float)FadeTotalRunTime / 1000));
                            }
                                break;
                            case Players.Player2:
                            {
                                Player2.volume = (int)(FadeVolumeAdjustment * ((float)FadeTotalRunTime / 1000));
                            }
                                break;
                        }
                    }
                    else if (FadeTotalRunTime >= FadeTime * 1000)
                    {
                        FadeTimer.Enabled = false;
                        FadeTimer.Stop();
                        InFade = false;
                        switch (PlayingPlayer)
                        {
                            case Players.Player1:
                                Player1.volume = Volume;
                                break;
                            case Players.Player2:
                                Player2.volume = Volume;
                                break;
                        }
                    }
                }
                else if (FadeState == FadeStates.Out)
                {
                    if (FadeTotalRunTime <= 0) { }
                    else if (FadeTotalRunTime < FadeTime * 1000)
                    {
                        switch (StoppingPlayer)
                        {
                            case Players.Player1:
                            {
                                Player1.volume = (int)(Volume - FadeVolumeAdjustment * ((float)FadeTotalRunTime / 1000));
                            }
                                break;
                            case Players.Player2:
                            {
                                Player2.volume = (int)(Volume - FadeVolumeAdjustment * ((float)FadeTotalRunTime / 1000));
                            }
                                break;
                        }
                    }
                    else if (FadeTotalRunTime >= FadeTime * 1000)
                    {
                        FadeTimer.Enabled = false;
                        FadeTimer.Stop();
                        InFade = false;
                        Player1.pause();
                        Player1.volume = 0;
                        Player2.pause();
                        Player2.volume = 0;
                    }
                }
                else if (FadeState == FadeStates.Up)
                {
                    if (FadeTotalRunTime < FadeTime * 1000)
                    {
                        switch (PlayingPlayer)
                        {
                            case Players.Player1:
                            {
                                Player1.volume = (int)(FadeVolumeAdjustment * ((float)FadeTotalRunTime / 1000)) + Volume;
                            }
                                break;
                            case Players.Player2:
                            {
                                Player2.volume = (int)(FadeVolumeAdjustment * ((float)FadeTotalRunTime / 1000)) + Volume;
                            }
                                break;
                        }
                    }
                    else if (FadeTotalRunTime >= FadeTime * 1000)
                    {
                        Volume = NewVolume;
                        FadeTimer.Enabled = false;
                        FadeTimer.Stop();
                        InFade = false;
                        switch (PlayingPlayer)
                        {
                            case Players.Player1:
                                Player1.volume = Volume;
                                break;
                            case Players.Player2:
                                Player2.volume = Volume;
                                break;
                        }
                    }
                }
                else if (FadeState == FadeStates.Down)
                {
                    if (FadeTotalRunTime <= 0) { }
                    else if (FadeTotalRunTime < FadeTime * 1000)
                    {
                        switch (PlayingPlayer)
                        {
                            case Players.Player1:
                            {
                                Player1.volume = (int)(Volume - FadeVolumeAdjustment * ((float)FadeTotalRunTime / 1000));
                            }
                                break;
                            case Players.Player2:
                            {
                                Player2.volume = (int)(Volume - FadeVolumeAdjustment * ((float)FadeTotalRunTime / 1000));
                            }
                                break;
                        }
                    }
                    else if (FadeTotalRunTime >= FadeTime * 1000)
                    {
                        FadeTimer.Enabled = false;
                        FadeTimer.Stop();
                        InFade = false;
                        switch (PlayingPlayer)
                        {
                            case Players.Player1:
                                Player1.volume = NewVolume;
                                break;
                            case Players.Player2:
                                Player2.volume = NewVolume;
                                break;
                        }
                        volume = NewVolume;
                    }
                }
                else
                {
                    FadeTimer.Enabled = false;
                    FadeTimer.Stop();
                    InFade = false;
                }
                FadeTotalRunTime += FadeTimerTickTime;
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("Fade timer tick : " + ex.Message));
            }
        }

        /// <summary>
        ///     Fades the player's volume up/down to the specified level.
        /// </summary>
        /// <param name="ANewVolume">The volume to fade to.</param>
        /// <returns>Returns true if fade starts.</returns>
        public bool Fade(int ANewVolume)
        {
            try
            {
                if (!InFade && !InCrossfade && PlayerState == PlayerStates.Playing)
                {
                    NewVolume = ANewVolume > 100 ? 100 : ANewVolume < 0 ? 0 : ANewVolume;
                    InFade = true;
                    FadeState = NewVolume < Volume ? FadeStates.Down : FadeStates.Up;
                    StartFade();
                }
            }
            catch (Exception ex)
            {
                OnError.Invoke(new ErrorEventArgs("Pause : " + ex.Message));
            }
            return false;
        }

        private delegate void PlayerReadyEvent(PlayerReadyEventArgs e);
        private delegate void PlayerErrorEvent(PlayerErrorEventArgs e);
    }

    public class ErrorEventArgs : EventArgs
    {
        public string Message;

        public ErrorEventArgs(string AMessage)
        {
            Message = AMessage;
        }
    }

    public class PlayerStartEventArgs : EventArgs
    {
        internal Players ThePlayer;
        public string TheSong;

        internal PlayerStartEventArgs(string ASong, Players APlayer)
        {
            TheSong = ASong;
            ThePlayer = APlayer;
        }
    }
    public class PlayerStopEventArgs : EventArgs
    {
        internal Players ThePlayer;
        public string TheSong;

        internal PlayerStopEventArgs(string ASong, Players APlayer)
        {
            TheSong = ASong;
            ThePlayer = APlayer;
        }
    }
    public class PlayerPausedEventArgs : EventArgs
    {
        internal Players ThePlayer;
        public string TheSong;

        internal PlayerPausedEventArgs(string ASong, Players APlayer)
        {
            TheSong = ASong;
            ThePlayer = APlayer;
        }
    }
    public class PlayerMediaEndedEventArgs : EventArgs
    {
        public string TheSong;

        public PlayerMediaEndedEventArgs(string ASong)
        {
            TheSong = ASong;
        }
    }

    internal class PlayerReadyEventArgs : EventArgs
    {
        public Players ThePlayer;

        public PlayerReadyEventArgs(Players APlayer)
        {
            ThePlayer = APlayer;
        }
    }
    internal class PlayerErrorEventArgs : EventArgs
    {
        public int StateNum;
        public Players ThePlayer;

        public PlayerErrorEventArgs(Players APlayer, int AStateNum)
        {
            ThePlayer = APlayer;
            StateNum = AStateNum;
        }
    }
}