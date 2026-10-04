window.assistantWindow = {
    // Enter sends, Shift+Enter is a newline. Blazor can't cancel a single keypress from the server,
    // so without this the browser inserts the newline Enter would normally add, and the text the
    // user just sent reappears in the box.
    bindEnterToSend: function (textarea) {
        if (!textarea || textarea.dataset.enterBound) {
            return;
        }

        textarea.dataset.enterBound = "true";
        textarea.addEventListener("keydown", function (e) {
            if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
                e.preventDefault();
            }
        });
    }
};
