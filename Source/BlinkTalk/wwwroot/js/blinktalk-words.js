// Word-suggestion and sentence scrolling for BlinkTalk.
//
// The suggestion words and the sentence each sit in one non-wrapping row that clips at the panel
// edges with no scroll bar (see .bt-words and .bt-sentence in blinktalk.css). The browser's own
// scrollIntoView does the scrolling — including for a right-to-left script, where it honours the
// page's dir.

// Bring a highlighted word (found by its data-word-index attribute, which stays stable across
// re-renders where an ElementReference would not) fully into view: centred in the panel when
// there is room on both sides, clamped to the panel edges otherwise, so there is never blank
// space at either end of the row. A missing word (the list changed since the highlight was set)
// is simply ignored.
export function scrollWordIntoView(wordIndex) {
    const word = document.querySelector(`[data-word-index="${wordIndex}"]`);
    if (!word) return;
    word.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
}

// Scroll the sentence to its end: the panel is a single fixed-height line that clips its
// overflow (no scroll bar), and the current-word box is always the last thing in it, so
// end-aligning it with the panel edge shows the whole latest tail of the sentence. Called after
// the text changes; a missing box (empty sentence shows the placeholder instead) is ignored.
export function scrollSentenceToEnd() {
    const current = document.querySelector(".bt-current-word");
    if (!current) return;
    current.scrollIntoView({ behavior: "auto", block: "nearest", inline: "end" });
}
