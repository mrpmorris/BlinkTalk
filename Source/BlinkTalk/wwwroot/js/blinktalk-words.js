// Word-suggestion scrolling for BlinkTalk.
//
// The suggestion words sit in one non-wrapping row that clips at the panel edges with no scroll
// bar (see .bt-words in blinktalk.css). When the scan highlights a word it must be brought fully
// into view: centred in the panel when there is room on both sides, clamped to the panel edges
// otherwise, so there is never blank space at either end of the row. The browser's own
// scrollIntoView does exactly that — including for a right-to-left script, where it honours the
// page's dir. The words are found by their data-word-index attribute, which stays stable across
// renders where an ElementReference would not.

// Scroll the word identified by data-word-index into view within the words panel (its nearest
// scrollable ancestor, which also happens to be the only one that can scroll). A missing word
// (the list changed since the highlight was set) is simply ignored.
export function scrollWordIntoView(wordIndex) {
    const word = document.querySelector(`[data-word-index="${wordIndex}"]`);
    if (!word) return;
    word.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
}
