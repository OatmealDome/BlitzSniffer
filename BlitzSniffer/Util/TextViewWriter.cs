using NStack;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using Terminal.Gui;

namespace BlitzSniffer.Util
{
    class TextViewWriter : TextWriter
    {
        private TextView View;
        private MethodInfo InsertTextMethod;

        public TextViewWriter(TextView view)
        {
            View = view;

            // Fetch the InsertText(ustring text) method. For whatever reason, there is no API for appending to the
            // TextView. "TextView.Text += newText" does work, but it doesn't scroll the TextView to the bottom
            // like I want it to. So, we're stuck with this gigantic hack for now.
            InsertTextMethod = view.GetType().GetMethod("InsertText", BindingFlags.Instance | BindingFlags.NonPublic,
                                                        null, new Type[] { typeof(ustring) }, null);
        }

        public override Encoding Encoding => Encoding.UTF8;

        private StringBuilder Builder = new StringBuilder();

        public override void Write(char value)
        {
            Builder.Append(value);

            // Only flushing when we get to a \n is probably better than calling InsertText with every single character.
            if (value == '\n')
            {
                string str = Builder.ToString();

                Application.MainLoop?.Invoke(() =>
                {
                    InsertTextMethod.Invoke(View, new object[] { ustring.Make(str) });
                    View.SetNeedsDisplay();
                });

                Builder.Clear();
            }
        }

    }
}
