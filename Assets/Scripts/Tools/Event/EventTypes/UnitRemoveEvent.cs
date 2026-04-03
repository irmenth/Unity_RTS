public struct UnitRemoveEvent
{
    public int oldID, newID;

    public UnitRemoveEvent(int oldID, int newID)
    {
        this.oldID = oldID;
        this.newID = newID;
    }
}
