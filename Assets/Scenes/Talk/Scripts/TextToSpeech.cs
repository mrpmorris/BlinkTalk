namespace Assets.Scripts
{
    public static class TextToSpeech
    {
        static TextToSpeech()
        {
            EasyTTSUtil.Initialize(EasyTTSUtil.UnitedKingdom);
        }

        public static void Speak(string text)
        {
            EasyTTSUtil.SpeechFlush(text + ".", volume: 1, rate: 0.4f, pitch: 0.6f);
        }
    }
}
