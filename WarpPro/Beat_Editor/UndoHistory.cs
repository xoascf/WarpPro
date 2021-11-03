using System.Collections.Generic;
using System.Linq;

namespace Beat_Editor
{
	public class UndoHistory
	{
		private class UndoableHistoryCollection
		{
			private List<IUndoable> list = new List<IUndoable>();

			private static readonly int MAX_UNDO = 50;

			public void Add(IUndoable item)
			{
				list.Add(item);
				while (list.Count > MAX_UNDO)
				{
					list.RemoveAt(0);
				}
			}

			public IUndoable ReCommitLast()
			{
				if (list.Count == 0)
				{
					return null;
				}
				IUndoable undoable = list.Last();
				list.Remove(undoable);
				return undoable.Commit();
			}

			public void Clear()
			{
				list.Clear();
			}
		}

		private UndoableHistoryCollection undoables = new UndoableHistoryCollection();

		private UndoableHistoryCollection redoables = new UndoableHistoryCollection();

		public static readonly UndoHistory Instance = new UndoHistory();

		public void Add(IUndoable change)
		{
			undoables.Add(change);
			redoables.Clear();
		}

		public bool UndoLast()
		{
			IUndoable undoable = undoables.ReCommitLast();
			if (undoable == null)
			{
				return false;
			}
			redoables.Add(undoable);
			return true;
		}

		public bool RedoLast()
		{
			IUndoable undoable = redoables.ReCommitLast();
			if (undoable == null)
			{
				return false;
			}
			undoables.Add(undoable);
			return true;
		}

		public void Clear()
		{
			undoables.Clear();
			redoables.Clear();
		}

		private UndoHistory()
		{
		}
	}
}
