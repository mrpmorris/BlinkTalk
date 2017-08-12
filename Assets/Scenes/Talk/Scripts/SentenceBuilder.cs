namespace Assets.Scripts
{
    public static class SentenceBuilder
    {
        private static string sentence = "";
        private static bool clearSentenceOnNextAdd = false;

        public static string GetSentence()
        {
            return sentence;
        }

        public static void AddWord(string word)
        {
            if (clearSentenceOnNextAdd)
                sentence = "";
            clearSentenceOnNextAdd = false;
            WordList.UpdateWordUsage(word);
            sentence += word + " ";
            TextToSpeech.Speak(word);
        }

        public static void SpeakSentence()
        {
            clearSentenceOnNextAdd = true;
            TextToSpeech.Speak(sentence);
            WordList.ResetState();
        }
    }
}
