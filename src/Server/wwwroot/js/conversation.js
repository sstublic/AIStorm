// JavaScript functions for conversation page

// Function to scroll an element into view
window.scrollIntoView = function (element) {
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'end' });
    }
};

// Function to focus the message textarea
window.focusMessageInput = function (element) {
    if (element) {
        setTimeout(() => {
            element.focus();
        }, 100);
    }
};

// Function to resize textarea automatically
window.setupAutoResizeTextarea = function (textareaId) {
    const textarea = document.getElementById(textareaId);
    if (!textarea) return;
    
    const adjustHeight = () => {
        textarea.style.height = 'auto';
        textarea.style.height = Math.min(textarea.scrollHeight, 200) + 'px';
    };
    
    textarea.addEventListener('input', adjustHeight);
    
    // Initial adjustment
    adjustHeight();
};

// Function to disable form submission on Enter (but allow Shift+Enter)
window.preventFormSubmitOnEnter = function (formId) {
    const form = document.getElementById(formId);
    if (!form) return;
    
    form.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
        }
    });
};
