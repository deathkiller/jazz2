using System;
using WebAssembly;

namespace Jazz2.Wasm
{
    public static class HtmlHelper
    {
        public static JSObject AddCanvas(string canvasId, int width, int height)
        {
            using (var document = (JSObject)Runtime.GetGlobalObject("document"))
            using (var container = (JSObject)document.Invoke("getElementById", "game-container")) {
                var canvas = (JSObject)document.Invoke("createElement", "canvas");
                canvas.SetObjectProperty("width", width);
                canvas.SetObjectProperty("height", height);
                canvas.SetObjectProperty("id", canvasId);
                container.Invoke("appendChild", canvas);
                return canvas;
            }
        }

        public static void AddHeader(int headerIndex, string text)
        {
            using (var document = (JSObject)Runtime.GetGlobalObject("document"))
            using (var body = (JSObject)document.GetObjectProperty("body"))
            using (var header = (JSObject)document.Invoke("createElement", $"h{headerIndex}"))
            using (var headerText = (JSObject)document.Invoke("createTextNode", text)) {
                header.Invoke("appendChild", headerText);
                body.Invoke("appendChild", header);
            }
        }

        public static void AddParagraph(string text)
        {
            using (var document = (JSObject)Runtime.GetGlobalObject("document"))
            using (var body = (JSObject)document.GetObjectProperty("body"))
            using (var paragraph = (JSObject)document.Invoke("createElement", "p")) {
                paragraph.SetObjectProperty("innerHTML", text);
                body.Invoke("appendChild", paragraph);
            }
        }

        public static void AddButton(string id, string text)
        {
            using (var document = (JSObject)Runtime.GetGlobalObject("document"))
            using (var body = (JSObject)document.GetObjectProperty("body"))
            using (var button = (JSObject)document.Invoke("createElement", "button")) {
                button.SetObjectProperty("innerHTML", text);
                button.SetObjectProperty("id", id);
                body.Invoke("appendChild", button);
            }
        }

        public static void AttachButtonOnClickEvent(string id, Action<JSObject> onClickAction)
        {
            using (var document = (JSObject)Runtime.GetGlobalObject("document"))
            using (var button = (JSObject)document.Invoke("getElementById", id)) {
                button.SetObjectProperty("onclick", onClickAction);
            }
        }
    }
}