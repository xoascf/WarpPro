namespace Beat_Editor
{
	public interface UndoHost
	{
		IUndoable ReCommit(IUndoable change);
	}
}
