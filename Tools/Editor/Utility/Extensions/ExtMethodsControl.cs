using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Editor
{
    public static class ExtMethodsControl
	{
		public static Control GetChildAtPointDeep(this Control control, Point pt, GetChildAtPointSkip skip)
		{
			Point globalPt = control.PointToScreen(pt);
			Control child = control.GetChildAtPoint(pt, skip);
			Control deeperChild = child;
			while (deeperChild != null)
			{
				child = deeperChild;
				deeperChild = deeperChild.GetChildAtPoint(deeperChild.PointToClient(globalPt), skip);
			}
			return deeperChild ?? child;
		}
		public static T GetControlAncestor<T>(this Control control) where T : class
		{
			while (control != null)
			{
				if (control is T) return control as T;
				control = control.Parent;
			}
			return null;
		}
		public static IEnumerable<T> GetControlAncestors<T>(this Control control) where T : class
		{
			while (control != null)
			{
				if (control is T) yield return control as T;
				control = control.Parent;
			}
			yield break;
		}

		public static U InvokeEx<T,U>(this T control, Func<T, U> func) where T : Control
		{
			return control.InvokeRequired ? (U)control.Invoke(func, control) : func(control);
		}
		public static void InvokeEx<T>(this T control, Action<T> func, bool waitForResult = true) where T : Control
		{
			if (waitForResult)
			{
				control.InvokeEx(c => { func(c); return c; });
			}
			else
			{
				// Perform an asynchronous invoke, if necessary
				if (control.InvokeRequired)
					control.BeginInvoke(func, control);
				else
					func(control);
			}
		}
		public static void InvokeEx<T>(this T control, Action action, bool waitForResult = true) where T : Control
		{
			control.InvokeEx(c => action(), waitForResult);
		}
	}
}
