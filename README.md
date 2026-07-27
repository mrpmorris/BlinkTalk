# BlinkTalk

Written for a friend with locked-in syndrome.

Get the latest releases here
* [Windows](https://github.com/mrpmorris/BlinkTalk/releases)
* [Android](https://play.google.com/store/apps/details?id=com.airsoftwarelimited.blinktalk.app)

BlinkTalk is a single-switch [AAC](https://en.wikipedia.org/wiki/Augmentative_and_alternative_communication)
(augmentative and alternative communication) app. A helper points the screen at the person they wish to
communicate with and taps the screen whenever the person indicates (blinks, looks up, whatever they prefer).
From that single signal, the person can spell out letters, pick whole words, and speak complete sentences aloud.

On Desktop computers the assistant can press the `Space` Bar on the keyboard, or click
the left mouse button anywhere on the app.

*All platforms allow the user to indicate using a facial gesture such as looking up or blinking.*

![Illustration](Docs/Images/Illustration.png)

## How it works

The screen continuously **scans** - it highlights one option at a time on a timer. A user or helper indication "selects"
whatever happens to be highlighted at that moment.

The whole screen is the button: the helper can tap anywhere on touch screen devices, click
anywhere on the screen using a mouse (desktop), press the `Space` bar on the keyboard,
or the app can observe the user via the device camera and detect when they indicate.

Selecting moves through a hierarchy of scanners:

1. **Section** — choose between picking a suggested word, opening the keyboard, or speaking the sentence.

![Illustration](Docs/Images/Demo-01-TopLevel.gif)

2. **Word suggestions** — instead of spelling a whole word, pick from predicted words.

![Illustration](Docs/Images/Demo-02-SelectWord.gif)


3. **Keyboard** — scan to a row of keys, then to a column, to land on a single letter.

![Illustration](Docs/Images/Demo-03-TypeWord.gif)

To keep typing fast, BlinkTalk predicts what the person is most likely to say next. A bundled dictionary
plus a word-sequence (n-gram) model learns the person's vocabulary and phrasing over time, so frequently
used words and natural word combinations are offered first.
