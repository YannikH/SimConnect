type webviewPostMessage = {type: string, data?: unknown};
export const PostMessage = (message: webviewPostMessage) => {
  if (window.chrome.webview) {
    window.chrome.webview.postMessage(message);
  }
}