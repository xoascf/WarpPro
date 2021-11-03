namespace Beat_Editor.Model
{
	public class UndoableBeatCollection : BeatCollection, IUndoable
	{
		private UndoHost master;

		public UndoableBeatCollection(UndoHost master, BeatCollection other)
			: base(other)
		{
			this.master = master;
		}

		public IUndoable Commit()
		{
			return master.ReCommit(this);
		}
	}
}
