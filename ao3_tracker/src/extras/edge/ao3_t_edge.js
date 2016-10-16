// Just a helper script for MS Edge

// If chrome isn't defined, check for browser or msBrowser and use those instead.
if (typeof msBrowser !== 'undefined') {
    chrome = msBrowser;
} else if (typeof browser !== 'undefined') {
    chrome = browser;
}