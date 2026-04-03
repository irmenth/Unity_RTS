public struct ObstacleRemoveEvent
{
    public int oldID, newID;

    public ObstacleRemoveEvent(int oldID, int newID)
    {
        this.oldID = oldID;
        this.newID = newID;
    }
}
