// JavaScript functions for conversation page

// Function to scroll an element into view
window.scrollIntoView = function (element) {
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'end' });
    }
};
