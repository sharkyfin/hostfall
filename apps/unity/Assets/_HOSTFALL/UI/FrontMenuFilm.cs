using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace Hostfall.UI
{
    public sealed class FrontMenuFilm : MonoBehaviour
    {
        private VisualElement image;
        private StyleBackground poster;
        private VideoPlayer movie;
        private RenderTexture frames;

        public void Initialize(VisualElement background)
        {
            image = background;
            poster = new StyleBackground(Resources.Load<Texture2D>("Hostfall2/FrontMenuPoster"));
            image.style.backgroundImage = poster;
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                Stop();
                return;
            }
            if (movie != null || Application.isBatchMode) return;
            frames = new RenderTexture(960, 540, 0, RenderTextureFormat.ARGB32);
            frames.Create();
            movie = gameObject.AddComponent<VideoPlayer>();
            movie.playOnAwake = false;
            movie.isLooping = true;
            movie.audioOutputMode = VideoAudioOutputMode.None;
            movie.source = VideoSource.Url;
            movie.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            movie.renderMode = VideoRenderMode.RenderTexture;
            movie.targetTexture = frames;
            movie.waitForFirstFrame = true;
            string path = Application.streamingAssetsPath + "/Hostfall2/FrontMenu.mp4";
            movie.url = Application.platform == RuntimePlatform.WebGLPlayer ? path : new Uri(path).AbsoluteUri;
            movie.prepareCompleted += Play;
            movie.errorReceived += Failed;
            movie.Prepare();
        }

        private void Play(VideoPlayer source)
        {
            if (source != movie) return;
            source.sendFrameReadyEvents = true;
            source.frameReady += FirstFrame;
            source.Play();
        }

        private void FirstFrame(VideoPlayer source, long frame)
        {
            if (source != movie) return;
            image.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(frames));
            source.frameReady -= FirstFrame;
            source.sendFrameReadyEvents = false;
        }

        private void Failed(VideoPlayer source, string message)
        {
            if (source != movie) return;
            Debug.LogWarning("Front menu film could not start. " + message, this);
            Stop();
        }

        private void Stop()
        {
            if (movie == null) return;
            movie.prepareCompleted -= Play;
            movie.frameReady -= FirstFrame;
            movie.errorReceived -= Failed;
            movie.Stop();
            movie.targetTexture = null;
            Destroy(movie);
            movie = null;
            image.style.backgroundImage = poster;
            frames.Release();
            Destroy(frames);
            frames = null;
        }

        private void OnDestroy() => Stop();
    }
}
